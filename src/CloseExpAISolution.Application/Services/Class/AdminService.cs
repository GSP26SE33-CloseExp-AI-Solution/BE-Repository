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
        OrderState.Paid_Processing.ToString(),
        OrderState.Ready_To_Ship.ToString(),
        OrderState.Delivered_Wait_Confirm.ToString(),
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
            .Where(o => RevenueStatuses.Contains(o.Status))
            .Sum(o => o.FinalAmount);

        var slaBreached = filteredOrders.Count(o =>
            !TerminalStatuses.Contains(o.Status) &&
            o.OrderDate <= now.AddHours(-2));

        var userCount = await _unitOfWork.Repository<User>().CountAsync();
        var activeSupermarketCount = await _unitOfWork.Repository<Supermarket>()
            .CountAsync(s => s.Status == "Active");

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
            .Where(o => o.OrderDate.Date >= fromDate && RevenueStatuses.Contains(o.Status))
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
            .Where(o => !TerminalStatuses.Contains(o.Status) && o.OrderDate <= thresholdTime)
            .OrderBy(o => o.OrderDate)
            .Take(safeTop)
            .Select(o => new AdminSlaAlertDto
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                Status = o.Status,
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
        return slots
            .OrderBy(x => x.StartTime)
            .Select(x => new AdminTimeSlotDto
            {
                TimeSlotId = x.DeliveryTimeSlotId,
                StartTime = x.StartTime,
                EndTime = x.EndTime
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
            EndTime = entity.EndTime
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

        return new AdminTimeSlotDto
        {
            TimeSlotId = entity.DeliveryTimeSlotId,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime
        };
    }

    public async Task<bool> DeleteTimeSlotAsync(Guid timeSlotId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<DeliveryTimeSlot>()
            .FirstOrDefaultAsync(x => x.DeliveryTimeSlotId == timeSlotId);

        if (entity == null)
            return false;

        _unitOfWork.Repository<DeliveryTimeSlot>().Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<AdminCollectionPointDto>> GetCollectionPointsAsync(CancellationToken cancellationToken = default)
    {
        var points = await _unitOfWork.Repository<CollectionPoint>().GetAllAsync();
        return points
            .OrderBy(x => x.Name)
            .Select(x => new AdminCollectionPointDto
            {
                CollectionId = x.CollectionId,
                Name = x.Name,
                AddressLine = x.AddressLine
            });
    }

    public async Task<AdminCollectionPointDto> CreateCollectionPointAsync(UpsertCollectionPointRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = new CollectionPoint
        {
            CollectionId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            AddressLine = request.AddressLine.Trim()
        };

        await _unitOfWork.Repository<CollectionPoint>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminCollectionPointDto
        {
            CollectionId = entity.CollectionId,
            Name = entity.Name,
            AddressLine = entity.AddressLine
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

        _unitOfWork.Repository<CollectionPoint>().Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminCollectionPointDto
        {
            CollectionId = entity.CollectionId,
            Name = entity.Name,
            AddressLine = entity.AddressLine
        };
    }

    public async Task<bool> DeleteCollectionPointAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<CollectionPoint>()
            .FirstOrDefaultAsync(x => x.CollectionId == collectionId);

        if (entity == null)
            return false;

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
        return units
            .OrderBy(x => x.Name)
            .Select(x => new AdminUnitDto
            {
                UnitId = x.UnitId,
                Name = x.Name,
                Type = x.Type,
                Symbol = x.Symbol,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
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
            UpdatedAt = entity.UpdatedAt
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

        return new AdminUnitDto
        {
            UnitId = entity.UnitId,
            Name = entity.Name,
            Type = entity.Type,
            Symbol = entity.Symbol,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
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

    public async Task<IEnumerable<AdminPromotionDto>> GetPromotionsAsync(CancellationToken cancellationToken = default)
    {
        var promotions = await _unitOfWork.Repository<Promotion>().GetAllAsync();
        return promotions
            .OrderByDescending(x => x.StartDate)
            .Select(MapPromotion);
    }

    public async Task<AdminPromotionDto> CreatePromotionAsync(CreatePromotionRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.EndDate < request.StartDate)
            throw new InvalidOperationException("EndDate phải lớn hơn hoặc bằng StartDate");

        var entity = new Promotion
        {
            PromotionId = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            DiscountType = request.DiscountType.Trim(),
            DiscountValue = request.DiscountValue,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status.Trim()
        };

        await _unitOfWork.Repository<Promotion>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapPromotion(entity);
    }

    public async Task<AdminPromotionDto?> UpdatePromotionAsync(Guid promotionId, UpdatePromotionRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<Promotion>()
            .FirstOrDefaultAsync(x => x.PromotionId == promotionId);

        if (entity == null)
            return null;

        if (request.StartDate.HasValue) entity.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) entity.EndDate = request.EndDate.Value;
        if (entity.EndDate < entity.StartDate)
            throw new InvalidOperationException("EndDate phải lớn hơn hoặc bằng StartDate");

        if (request.CategoryId.HasValue) entity.CategoryId = request.CategoryId.Value;
        if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.DiscountType)) entity.DiscountType = request.DiscountType.Trim();
        if (request.DiscountValue.HasValue) entity.DiscountValue = request.DiscountValue.Value;
        if (!string.IsNullOrWhiteSpace(request.Status)) entity.Status = request.Status.Trim();

        _unitOfWork.Repository<Promotion>().Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapPromotion(entity);
    }

    public async Task<AdminPromotionDto?> UpdatePromotionStatusAsync(Guid promotionId, string status, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status is required", nameof(status));

        var entity = await _unitOfWork.Repository<Promotion>()
            .FirstOrDefaultAsync(x => x.PromotionId == promotionId);

        if (entity == null)
            return null;

        entity.Status = status.Trim();
        _unitOfWork.Repository<Promotion>().Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapPromotion(entity);
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
                OriginalPrice = x.OriginalPrice,
                SuggestedUnitPrice = x.SuggestedUnitPrice,
                FinalPrice = x.FinalPrice,
                MarketAvgPrice = x.MarketAvgPrice,
                AiConfidence = x.AIConfidence,
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

    private static AdminPromotionDto MapPromotion(Promotion x) => new()
    {
        PromotionId = x.PromotionId,
        CategoryId = x.CategoryId,
        Name = x.Name,
        DiscountType = x.DiscountType,
        DiscountValue = x.DiscountValue,
        StartDate = x.StartDate,
        EndDate = x.EndDate,
        Status = x.Status
    };

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
