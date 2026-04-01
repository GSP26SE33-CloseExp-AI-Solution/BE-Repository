using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class AdminService : IAdminService
{
    private static readonly HashSet<string> RevenueStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        OrderState.PaidProcessing.ToString(),
        OrderState.ReadyToShip.ToString(),
        OrderState.DeliveredWaitConfirm.ToString(),
        OrderState.Completed.ToString()
    };

    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        OrderState.Canceled.ToString(),
        OrderState.Refunded.ToString(),
        OrderState.Failed.ToString(),
        OrderState.Completed.ToString()
    };

    private readonly IUnitOfWork _unitOfWork;

    public AdminService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminDashboardOverviewDto> GetDashboardOverviewAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var orders = await _unitOfWork.Repository<Order>().GetAllAsync();

        var filteredOrders = orders.Where(o =>
            (!fromUtc.HasValue || o.OrderDate >= fromUtc.Value) &&
            (!toUtc.HasValue || o.OrderDate <= toUtc.Value));

        var revenue = filteredOrders
            .Where(o => RevenueStatuses.Contains(o.Status.ToString()))
            .Sum(o => o.FinalAmount);

        var slaBreached = filteredOrders.Count(o =>
            !TerminalStatuses.Contains(o.Status.ToString()) &&
            o.OrderDate <= now.AddHours(-2));

        var userCount = await _unitOfWork.Repository<User>().CountAsync();
        var activeSupermarketCount = await _unitOfWork.Repository<Supermarket>()
            .CountAsync(s => s.Status == SupermarketState.Active);

        return new AdminDashboardOverviewDto
        {
            TotalRevenue = revenue,
            TotalOrders = filteredOrders.Count(),
            TotalUsers = userCount,
            ActiveSupermarkets = activeSupermarketCount,
            SlaBreachedOrders = slaBreached
        };
    }

    public async Task<IEnumerable<AdminRevenueTrendPointDto>> GetRevenueTrendAsync(int days, CancellationToken cancellationToken = default)
    {
        var safeDays = Math.Clamp(days, 1, 90);
        var fromDate = DateTime.UtcNow.Date.AddDays(-(safeDays - 1));

        var orders = await _unitOfWork.Repository<Order>().GetAllAsync();
        var grouped = orders
            .Where(o => o.OrderDate.Date >= fromDate && RevenueStatuses.Contains(o.Status.ToString()))
            .GroupBy(o => o.OrderDate.Date)
            .ToDictionary(
                g => g.Key,
                g => new AdminRevenueTrendPointDto
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.FinalAmount),
                    OrderCount = g.Count()
                });

        var result = new List<AdminRevenueTrendPointDto>(safeDays);
        for (var i = 0; i < safeDays; i++)
        {
            var date = fromDate.AddDays(i);
            if (grouped.TryGetValue(date, out var point))
            {
                result.Add(point);
            }
            else
            {
                result.Add(new AdminRevenueTrendPointDto
                {
                    Date = date,
                    Revenue = 0,
                    OrderCount = 0
                });
            }
        }

        return result;
    }

    public async Task<IEnumerable<AdminSlaAlertDto>> GetSlaAlertsAsync(int thresholdMinutes, int top, CancellationToken cancellationToken = default)
    {
        var safeThreshold = Math.Clamp(thresholdMinutes, 1, 24 * 60);
        var safeTop = Math.Clamp(top, 1, 200);
        var thresholdTime = DateTime.UtcNow.AddMinutes(-safeThreshold);

        var orders = await _unitOfWork.Repository<Order>().GetAllAsync();

        return orders
            .Where(o => !TerminalStatuses.Contains(o.Status.ToString()) && o.OrderDate <= thresholdTime)
            .OrderBy(o => o.OrderDate)
            .Take(safeTop)
            .Select(o => new AdminSlaAlertDto
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                Status = o.Status.ToString(),
                OrderDate = o.OrderDate,
                MinutesLate = (int)Math.Max(0, (DateTime.UtcNow - o.OrderDate).TotalMinutes - safeThreshold),
                DeliveryType = o.DeliveryType,
                UserId = o.UserId
            })
            .ToList();
    }

    public async Task<IEnumerable<AdminTimeSlotDto>> GetTimeSlotsAsync(CancellationToken cancellationToken = default)
    {
        var slots = await _unitOfWork.Repository<DeliveryTimeSlot>().GetAllAsync();
        var orderCountBySlot = await GetOrderTimeSlotCountsAsync(cancellationToken);
        return slots
            .OrderBy(x => x.StartTime)
            .Select(x => new AdminTimeSlotDto
            {
                TimeSlotId = x.DeliveryTimeSlotId,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                RelatedOrderCount = orderCountBySlot.TryGetValue(x.DeliveryTimeSlotId, out var c) ? c : 0
            });
    }

    public async Task<AdminTimeSlotDto> CreateTimeSlotAsync(UpsertTimeSlotRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateTimeSlot(request.StartTime, request.EndTime);
        await EnsureNoOverlappingTimeSlotAsync(null, request.StartTime, request.EndTime);

        var entity = new DeliveryTimeSlot
        {
            DeliveryTimeSlotId = Guid.NewGuid(),
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        await _unitOfWork.Repository<DeliveryTimeSlot>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminTimeSlotDto
        {
            TimeSlotId = entity.DeliveryTimeSlotId,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            RelatedOrderCount = 0
        };
    }

    public async Task<AdminTimeSlotDto?> UpdateTimeSlotAsync(Guid timeSlotId, UpsertTimeSlotRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateTimeSlot(request.StartTime, request.EndTime);

        var entity = await _unitOfWork.Repository<DeliveryTimeSlot>()
            .FirstOrDefaultAsync(x => x.DeliveryTimeSlotId == timeSlotId);

        if (entity == null)
            return null;

        await EnsureNoOverlappingTimeSlotAsync(timeSlotId, request.StartTime, request.EndTime);

        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;

        _unitOfWork.Repository<DeliveryTimeSlot>().Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var relatedOrders = await _unitOfWork.Repository<Order>().CountAsync(o => o.TimeSlotId == timeSlotId);

        return new AdminTimeSlotDto
        {
            TimeSlotId = entity.DeliveryTimeSlotId,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            RelatedOrderCount = relatedOrders
        };
    }

    public async Task<bool> DeleteTimeSlotAsync(Guid timeSlotId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<DeliveryTimeSlot>()
            .FirstOrDefaultAsync(x => x.DeliveryTimeSlotId == timeSlotId);

        if (entity == null)
            return false;

        var orderRefCount = await _unitOfWork.Repository<Order>()
            .CountAsync(o => o.TimeSlotId == timeSlotId);
        var deliveryGroupRefCount = await _unitOfWork.Repository<DeliveryGroup>()
            .CountAsync(g => g.TimeSlotId == timeSlotId);

        if (orderRefCount > 0 || deliveryGroupRefCount > 0)
        {
            throw new InvalidOperationException(
                $"Không thể xóa khung giờ vì đang được sử dụng bởi {orderRefCount} đơn hàng và {deliveryGroupRefCount} nhóm giao.");
        }

        _unitOfWork.Repository<DeliveryTimeSlot>().Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<AdminCollectionPointDto>> GetCollectionPointsAsync(CancellationToken cancellationToken = default)
    {
        var points = await _unitOfWork.Repository<CollectionPoint>().GetAllAsync();
        var orderCountByCollection = await GetOrderCollectionCountsAsync(cancellationToken);
        return points
            .OrderBy(x => x.Name)
            .Select(x => new AdminCollectionPointDto
            {
                CollectionId = x.CollectionId,
                Name = x.Name,
                AddressLine = x.AddressLine,
                Latitude = x.Latitude ?? 0,
                Longitude = x.Longitude ?? 0,
                RelatedOrderCount = orderCountByCollection.TryGetValue(x.CollectionId, out var c) ? c : 0
            });
    }

    public async Task<AdminCollectionPointDto> CreateCollectionPointAsync(UpsertCollectionPointRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = new CollectionPoint
        {
            CollectionId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            AddressLine = request.AddressLine.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        await _unitOfWork.Repository<CollectionPoint>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminCollectionPointDto
        {
            CollectionId = entity.CollectionId,
            Name = entity.Name,
            AddressLine = entity.AddressLine,
            Latitude = entity.Latitude ?? 0,
            Longitude = entity.Longitude ?? 0,
            RelatedOrderCount = 0
        };
    }

    public async Task<AdminCollectionPointDto?> UpdateCollectionPointAsync(Guid collectionId, UpsertCollectionPointRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<CollectionPoint>()
            .FirstOrDefaultAsync(x => x.CollectionId == collectionId);

        if (entity == null)
            return null;

        entity.Name = request.Name.Trim();
        entity.AddressLine = request.AddressLine.Trim();
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;

        _unitOfWork.Repository<CollectionPoint>().Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var relatedOrders = await _unitOfWork.Repository<Order>().CountAsync(o => o.CollectionId == collectionId);

        return new AdminCollectionPointDto
        {
            CollectionId = entity.CollectionId,
            Name = entity.Name,
            AddressLine = entity.AddressLine,
            Latitude = entity.Latitude ?? 0,
            Longitude = entity.Longitude ?? 0,
            RelatedOrderCount = relatedOrders
        };
    }

    public async Task<bool> DeleteCollectionPointAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<CollectionPoint>()
            .FirstOrDefaultAsync(x => x.CollectionId == collectionId);

        if (entity == null)
            return false;

        var orderRefCount = await _unitOfWork.Repository<Order>()
            .CountAsync(o => o.CollectionId == collectionId);

        if (orderRefCount > 0)
        {
            throw new InvalidOperationException(
                $"Không thể xóa điểm tập kết vì đang được sử dụng bởi {orderRefCount} đơn hàng.");
        }

        _unitOfWork.Repository<CollectionPoint>().Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<AdminSystemConfigDto>> GetSystemConfigsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.Repository<SystemConfig>().GetAllAsync();
        return items
            .OrderBy(x => x.ConfigKey)
            .Select(x => new AdminSystemConfigDto
            {
                ConfigKey = x.ConfigKey,
                ConfigValue = x.ConfigValue,
                UpdatedAt = x.UpdatedAt
            });
    }

    public async Task<AdminSystemConfigDto> UpsertSystemConfigAsync(string configKey, UpsertSystemConfigRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configKey))
            throw new ArgumentException("Config key is required", nameof(configKey));

        var normalizedKey = configKey.Trim();
        var entity = await _unitOfWork.Repository<SystemConfig>()
            .FirstOrDefaultAsync(x => x.ConfigKey == normalizedKey);

        if (entity == null)
        {
            entity = new SystemConfig
            {
                ConfigKey = normalizedKey,
                ConfigValue = request.ConfigValue,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<SystemConfig>().AddAsync(entity);
        }
        else
        {
            entity.ConfigValue = request.ConfigValue;
            entity.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<SystemConfig>().Update(entity);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminSystemConfigDto
        {
            ConfigKey = entity.ConfigKey,
            ConfigValue = entity.ConfigValue,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<IEnumerable<AdminUnitDto>> GetUnitsAsync(CancellationToken cancellationToken = default)
    {
        var units = await _unitOfWork.Repository<UnitOfMeasure>().GetAllAsync();
        var stockLots = await _unitOfWork.Repository<StockLot>().GetAllAsync();
        var lotCountByUnit = stockLots
            .GroupBy(s => s.UnitId)
            .ToDictionary(g => g.Key, g => g.Count());

        return units
            .OrderBy(x => x.Name)
            .Select(x =>
            {
                var count = lotCountByUnit.TryGetValue(x.UnitId, out var c) ? c : 0;
                return new AdminUnitDto
                {
                    UnitId = x.UnitId,
                    Name = x.Name,
                    Type = x.Type,
                    Symbol = x.Symbol,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    RelatedStockLotCount = count,
                    IsInUse = count > 0
                };
            });
    }

    public async Task<AdminUnitDto> CreateUnitAsync(UpsertUnitRequestDto request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entity = new UnitOfMeasure
        {
            UnitId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            Symbol = request.Symbol.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _unitOfWork.Repository<UnitOfMeasure>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminUnitDto
        {
            UnitId = entity.UnitId,
            Name = entity.Name,
            Type = entity.Type,
            Symbol = entity.Symbol,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            RelatedStockLotCount = 0,
            IsInUse = false
        };
    }

    public async Task<AdminUnitDto?> UpdateUnitAsync(Guid unitId, UpsertUnitRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<UnitOfMeasure>()
            .FirstOrDefaultAsync(x => x.UnitId == unitId);

        if (entity == null)
            return null;

        entity.Name = request.Name.Trim();
        entity.Type = request.Type.Trim();
        entity.Symbol = request.Symbol.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<UnitOfMeasure>().Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var relatedLotCount = await _unitOfWork.Repository<StockLot>().CountAsync(s => s.UnitId == unitId);

        return new AdminUnitDto
        {
            UnitId = entity.UnitId,
            Name = entity.Name,
            Type = entity.Type,
            Symbol = entity.Symbol,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            RelatedStockLotCount = relatedLotCount,
            IsInUse = relatedLotCount > 0
        };
    }

    public async Task<bool> DeleteUnitAsync(Guid unitId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<UnitOfMeasure>()
            .FirstOrDefaultAsync(x => x.UnitId == unitId);

        if (entity == null)
            return false;

        _unitOfWork.Repository<UnitOfMeasure>().Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PaginatedResult<AdminAiPriceHistoryDto>> GetAiPriceHistoriesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, pageNumber);
        var safeSize = Math.Clamp(pageSize, 1, 200);

        var items = (await _unitOfWork.Repository<PricingHistory>().GetAllAsync())
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var pageItems = items
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .Select(x => new AdminAiPriceHistoryDto
            {
                AiPriceId = x.AIPriceId,
                LotId = x.LotId,
                SuggestedPrice = x.SuggestedPrice,
                MarketAvgPrice = x.MarketAvgPrice ?? 0,
                AiConfidence = (float)x.AIConfidence,
                AcceptedSuggestion = x.AcceptedSuggestion,
                ConfirmedBy = x.ConfirmedBy,
                ConfirmedAt = x.ConfirmedAt,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        return new PaginatedResult<AdminAiPriceHistoryDto>
        {
            Items = pageItems,
            TotalResult = items.Count,
            Page = safePage,
            PageSize = safeSize
        };
    }

    public async Task<PaginatedResult<AdminOrderListItemDto>> GetOrdersAsync(
        AdminOrderQueryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var (orders, total) = await _unitOfWork.OrderRepository.GetAdminPagedAsync(
            request.FromUtc,
            request.ToUtc,
            request.Status,
            request.DeliveryType,
            request.UserId,
            request.TimeSlotId,
            request.CollectionId,
            request.DeliveryGroupId,
            request.Search,
            request.SortBy,
            request.SortDir,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = orders.Select(o => new AdminOrderListItemDto
        {
            OrderId = o.OrderId,
            OrderCode = o.OrderCode,
            Status = o.Status.ToString(),
            OrderDate = o.OrderDate,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            DeliveryType = o.DeliveryType,
            TotalAmount = o.TotalAmount,
            DiscountAmount = o.DiscountAmount,
            FinalAmount = o.FinalAmount,
            DeliveryFee = o.DeliveryFee,
            UserId = o.UserId,
            UserName = o.User?.FullName,
            TimeSlotId = o.TimeSlotId,
            TimeSlotDisplay = o.DeliveryTimeSlot != null
                ? $"{o.DeliveryTimeSlot.StartTime:hh\\:mm} - {o.DeliveryTimeSlot.EndTime:hh\\:mm}"
                : null,
            CollectionId = o.CollectionId,
            CollectionPointName = o.CollectionPoint?.Name,
            DeliveryGroupId = o.DeliveryGroupId
        }).ToList();

        return new PaginatedResult<AdminOrderListItemDto>
        {
            Items = items,
            TotalResult = total,
            Page = Math.Max(1, request.PageNumber),
            PageSize = Math.Clamp(request.PageSize, 1, 200)
        };
    }

    private async Task<Dictionary<Guid, int>> GetOrderTimeSlotCountsAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _unitOfWork.Repository<Order>().GetAllAsync();
        return orders.GroupBy(o => o.TimeSlotId).ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task<Dictionary<Guid, int>> GetOrderCollectionCountsAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _unitOfWork.Repository<Order>().GetAllAsync();
        return orders
            .Where(o => o.CollectionId.HasValue)
            .GroupBy(o => o.CollectionId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task EnsureNoOverlappingTimeSlotAsync(Guid? currentId, TimeSpan start, TimeSpan end)
    {
        var slots = await _unitOfWork.Repository<DeliveryTimeSlot>().GetAllAsync();

        var hasOverlap = slots.Any(slot =>
            (!currentId.HasValue || slot.DeliveryTimeSlotId != currentId.Value) &&
            start < slot.EndTime &&
            end > slot.StartTime);

        if (hasOverlap)
            throw new InvalidOperationException("Khung giờ bị trùng với khung giờ hiện có");
    }

    private static void ValidateTimeSlot(TimeSpan start, TimeSpan end)
    {
        if (end <= start)
            throw new InvalidOperationException("EndTime phải lớn hơn StartTime");
    }
}








