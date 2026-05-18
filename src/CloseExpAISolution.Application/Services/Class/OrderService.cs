using AutoMapper;
using CloseExpAISolution.Application;
using CloseExpAISolution.Application.Configuration;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Geo;
using CloseExpAISolution.Application.Policies;
using CloseExpAISolution.Application.Services;
using CloseExpAISolution.Application.Services.Fulfillment;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CloseExpAISolution.Application.Services.Class;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPromotionService _promotionService;
    private readonly IPromotionUsageService _promotionUsageService;
    private readonly IOptions<PickupSearchOptions> _pickupSearchOptions;
    private readonly IOrderNotificationPublisher _orderNotificationPublisher;
    private readonly OrderItemUnitConverter _orderItemUnitConverter;
    private readonly OrderStockQuantityHelper _orderStockQuantityHelper;
    private readonly PurchaseUnitOrderHelper _purchaseUnitHelper;

    public OrderService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPromotionService promotionService,
        IPromotionUsageService promotionUsageService,
        IOptions<PickupSearchOptions> pickupSearchOptions,
        IOrderNotificationPublisher orderNotificationPublisher,
        OrderItemUnitConverter orderItemUnitConverter,
        OrderStockQuantityHelper orderStockQuantityHelper,
        PurchaseUnitOrderHelper purchaseUnitHelper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _promotionService = promotionService;
        _promotionUsageService = promotionUsageService;
        _pickupSearchOptions = pickupSearchOptions;
        _orderNotificationPublisher = orderNotificationPublisher;
        _orderItemUnitConverter = orderItemUnitConverter;
        _orderStockQuantityHelper = orderStockQuantityHelper;
        _purchaseUnitHelper = purchaseUnitHelper;
    }

    public async Task<IEnumerable<DeliveryTimeSlotDto>> GetDeliveryTimeSlotsAsync(CancellationToken cancellationToken = default)
    {
        var slots = await _unitOfWork.Repository<DeliveryTimeSlot>().GetAllAsync();
        var orderCountBySlot = await GetOrderTimeSlotCountsAsync(cancellationToken);

        return slots
            .OrderBy(x => x.StartTime)
            .Select(x => new DeliveryTimeSlotDto
            {
                TimeSlotId = x.DeliveryTimeSlotId,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                DisplayTimeRange = $"{x.StartTime:hh\\:mm} - {x.EndTime:hh\\:mm}",
                RelatedOrderCount = orderCountBySlot.TryGetValue(x.DeliveryTimeSlotId, out var c) ? c : 0
            })
            .ToList();
    }

    public async Task<IEnumerable<CollectionPointDto>> GetCollectionPointsAsync(CancellationToken cancellationToken = default)
    {
        var points = await _unitOfWork.Repository<CollectionPoint>().GetAllAsync();
        var orderCountByCollection = await GetOrderCollectionCountsAsync(cancellationToken);

        return points
            .OrderBy(x => x.Name)
            .Select(x => new CollectionPointDto
            {
                CollectionPointId = x.CollectionId,
                Name = x.Name,
                Address = x.AddressLine,
                RelatedOrderCount = orderCountByCollection.TryGetValue(x.CollectionId, out var c) ? c : 0,
                Latitude = x.Latitude,
                Longitude = x.Longitude
            })
            .ToList();
    }

    public async Task<IEnumerable<CollectionPointDto>> GetCollectionPointsNearbyAsync(
        NearbyCollectionPointsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var opts = _pickupSearchOptions.Value;
        var radiusKm = request.RadiusKm ?? opts.DefaultRadiusKm;
        if (radiusKm <= 0)
            radiusKm = opts.DefaultRadiusKm;
        if (radiusKm > opts.MaxRadiusKm)
            radiusKm = opts.MaxRadiusKm;

        var refLat = (double)request.Latitude;
        var refLng = (double)request.Longitude;

        var (minLat, maxLat, minLng, maxLng) = PickupSearchGeo.ComputeBoundingBox(
            request.Latitude,
            request.Longitude,
            radiusKm);

        // Step 1: EF-translatable bounding box (superset of the circle).
        var points = await _unitOfWork.Repository<CollectionPoint>().FindAsync(p =>
            p.Latitude != null
            && p.Longitude != null
            && p.Latitude >= minLat
            && p.Latitude <= maxLat
            && p.Longitude >= minLng
            && p.Longitude <= maxLng);
        var orderCountByCollection = await GetOrderCollectionCountsAsync(cancellationToken);

        var list = new List<CollectionPointDto>();
        foreach (var x in points)
        {
            if (x.Latitude is null || x.Longitude is null)
                continue;

            var dKm = PickupSearchGeo.HaversineDistanceKm(refLat, refLng, (double)x.Latitude.Value, (double)x.Longitude.Value);
            if (dKm > radiusKm)
                continue;

            list.Add(new CollectionPointDto
            {
                CollectionPointId = x.CollectionId,
                Name = x.Name,
                Address = x.AddressLine,
                RelatedOrderCount = orderCountByCollection.TryGetValue(x.CollectionId, out var c) ? c : 0,
                DistanceKm = dKm,
                Latitude = x.Latitude,
                Longitude = x.Longitude
            });
        }

        return list
            .OrderBy(p => p.DistanceKm ?? double.MaxValue)
            .ThenBy(p => p.Name)
            .ToList();
    }

    public async Task<IEnumerable<CustomerAddressDto>> GetCustomerAddressesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var addresses = await _unitOfWork.Repository<CustomerAddress>()
            .FindAsync(x => x.UserId == userId);

        return addresses
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.RecipientName)
            .Select(x => new CustomerAddressDto
            {
                AddressId = x.CustomerAddressId,
                RecipientName = x.RecipientName,
                Phone = x.Phone,
                AddressLine = x.AddressLine,
                IsDefault = x.IsDefault
            })
            .ToList();
    }

    public async Task<(IEnumerable<OrderResponseDto> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = await _unitOfWork.OrderRepository.GetAllAsync(cancellationToken);
        var list = all.ToList();
        var total = list.Count;
        var items = list
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(o => _mapper.Map<OrderResponseDto>(o))
            .ToList();
        return (items, total);
    }

    public async Task<OrderResponseDto?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.OrderRepository.GetByOrderIdAsync(orderId, cancellationToken);
        return order == null ? null : _mapper.Map<OrderResponseDto>(order);
    }

    public async Task<OrderResponseDto?> GetByIdWithDetailsAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken);
        return order == null ? null : _mapper.Map<OrderResponseDto>(order);
    }

    public async Task<OrderResponseDto> CreateAsync(CreateOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        var deliveryType = DeliveryMethod.NormalizeOrThrow(request.DeliveryType);
        OrderDeliveryLocationValidator.ValidateOrThrow(deliveryType, request.CollectionId, request.AddressId);
        await ValidateOrderLotsForCreationAsync(request.OrderItems, cancellationToken);

        var orderId = Guid.NewGuid();
        var orderCode = "ORD-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var orderPlacedAt = DateTime.UtcNow;
        var requestedDeliveryDate = ResolveRequestedDeliveryDate(request.DeliveryDate, orderPlacedAt);

        var order = new Order
        {
            OrderId = orderId,
            OrderCode = orderCode,
            UserId = request.UserId,
            TimeSlotId = request.TimeSlotId,
            CollectionId = request.CollectionId,
            DeliveryType = deliveryType,
            TotalAmount = request.TotalAmount,
            DiscountAmount = request.DiscountAmount,
            FinalAmount = request.FinalAmount,
            DeliveryFee = request.DeliveryFee,
            SystemUsageFeeAmount = 0m,
            Status = Enum.Parse<OrderState>(request.Status),
            OrderDate = requestedDeliveryDate,
            AddressId = request.AddressId,
            PromotionId = request.PromotionId,
            DeliveryGroupId = request.DeliveryGroupId,
            DeliveryNote = request.DeliveryNote,
            CancelDeadline = request.CancelDeadline,
            CreatedAt = orderPlacedAt,
            UpdatedAt = orderPlacedAt
        };

        if (request.PromotionId.HasValue)
        {
            var validation = await _promotionService.ValidatePromotionAsync(
                request.UserId,
                new ValidatePromotionRequestDto
                {
                    PromotionId = request.PromotionId,
                    TotalAmount = request.TotalAmount
                },
                cancellationToken);

            if (!validation.IsValid || !validation.PromotionId.HasValue)
                throw new InvalidOperationException(validation.Message);

            order.PromotionId = validation.PromotionId.Value;
            order.DiscountAmount = validation.DiscountAmount;
        }

        order.SystemUsageFeeAmount = await GetOrderSystemUsageFeeVndAsync(cancellationToken);
        order.FinalAmount = OrderTotalsHelper.ComputeFinalAmount(
            order.TotalAmount,
            order.DiscountAmount,
            order.DeliveryFee,
            order.SystemUsageFeeAmount);

        var convertedItems = await _orderItemUnitConverter.ConvertCreateItemsToProductUnitAsync(
            request.OrderItems,
            cancellationToken);

        foreach (var item in convertedItems)
        {
            var totalPrice = item.Quantity * item.UnitPrice;
            order.OrderItems.Add(new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = orderId,
                LotId = item.LotId,
                PurchaseUnitId = item.PurchaseUnitId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = totalPrice
            });
        }

        await _unitOfWork.OrderRepository.AddAsync(order, cancellationToken);
        await TryAutoAssignDeliveryGroupAsync(order, cancellationToken);
        await _orderNotificationPublisher.PublishOrderPlacedAsync(order.OrderId, order.UserId, order.OrderCode, cancellationToken);
        await TryCreateStatusLogAsync(order.OrderId, order.Status, order.Status, "system", "Order created", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await TryRecordPromotionUsageAsync(order, cancellationToken);

        var created = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken);
        return _mapper.Map<OrderResponseDto>(created!);
    }

    public async Task UpdateAsync(Guid orderId, UpdateOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order not found: {orderId}");

        var oldStatus = order.Status;

        if (request.TimeSlotId.HasValue) order.TimeSlotId = request.TimeSlotId.Value;

        if (request.DeliveryType != null)
        {
            var dt = DeliveryMethod.NormalizeOrThrow(request.DeliveryType);
            order.DeliveryType = dt;
            if (dt == DeliveryMethod.Delivery)
                order.CollectionId = null;
            else
                order.AddressId = null;
        }

        if (request.CollectionId.HasValue && order.DeliveryType == DeliveryMethod.Pickup)
            order.CollectionId = request.CollectionId;
        if (request.TotalAmount.HasValue) order.TotalAmount = request.TotalAmount.Value;
        if (request.Status != null) order.Status = Enum.Parse<OrderState>(request.Status);
        if (request.AddressId.HasValue && order.DeliveryType == DeliveryMethod.Delivery)
            order.AddressId = request.AddressId;
        if (request.PromotionId.HasValue) order.PromotionId = request.PromotionId;
        if (request.DeliveryGroupId.HasValue) order.DeliveryGroupId = request.DeliveryGroupId;
        if (request.DeliveryNote != null) order.DeliveryNote = request.DeliveryNote;
        if (request.DiscountAmount.HasValue) order.DiscountAmount = request.DiscountAmount.Value;
        if (request.FinalAmount.HasValue) order.FinalAmount = request.FinalAmount.Value;
        if (request.DeliveryFee.HasValue) order.DeliveryFee = request.DeliveryFee.Value;
        if (request.CancelDeadline.HasValue) order.CancelDeadline = request.CancelDeadline;

        if (request.PromotionId.HasValue)
        {
            var validation = await _promotionService.ValidatePromotionAsync(
                order.UserId,
                new ValidatePromotionRequestDto
                {
                    PromotionId = request.PromotionId,
                    TotalAmount = order.TotalAmount
                },
                cancellationToken);

            if (!validation.IsValid || !validation.PromotionId.HasValue)
                throw new InvalidOperationException(validation.Message);

            order.PromotionId = validation.PromotionId.Value;
            order.DiscountAmount = validation.DiscountAmount;
        }

        OrderDeliveryLocationValidator.ValidateOrThrow(order.DeliveryType, order.CollectionId, order.AddressId);

        if (request.OrderItems != null && request.OrderItems.Count > 0)
        {
            var convertedItems = await _orderItemUnitConverter.ConvertCreateItemsToProductUnitAsync(
                request.OrderItems.Select(dto => new CreateOrderItemDto
                {
                    LotId = dto.LotId,
                    PurchaseUnitId = dto.PurchaseUnitId,
                    Quantity = dto.Quantity,
                    UnitPrice = dto.UnitPrice
                }).ToList(),
                cancellationToken);

            order.OrderItems.Clear();
            for (var i = 0; i < request.OrderItems.Count; i++)
            {
                var dto = request.OrderItems[i];
                var item = convertedItems[i];
                var totalPrice = item.Quantity * item.UnitPrice;
                order.OrderItems.Add(new OrderItem
                {
                    OrderItemId = dto.OrderItemId == Guid.Empty ? Guid.NewGuid() : dto.OrderItemId,
                    OrderId = orderId,
                    LotId = item.LotId,
                    PurchaseUnitId = item.PurchaseUnitId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = totalPrice
                });
            }
        }

        if (request.PromotionId.HasValue)
        {
            order.SystemUsageFeeAmount = await GetOrderSystemUsageFeeVndAsync(cancellationToken);
            order.FinalAmount = OrderTotalsHelper.ComputeFinalAmount(
                order.TotalAmount,
                order.DiscountAmount,
                order.DeliveryFee,
                order.SystemUsageFeeAmount);
        }
        else if (!request.FinalAmount.HasValue &&
                 (request.TotalAmount.HasValue || request.DeliveryFee.HasValue || request.DiscountAmount.HasValue))
        {
            order.SystemUsageFeeAmount = await GetOrderSystemUsageFeeVndAsync(cancellationToken);
            order.FinalAmount = OrderTotalsHelper.ComputeFinalAmount(
                order.TotalAmount,
                order.DiscountAmount,
                order.DeliveryFee,
                order.SystemUsageFeeAmount);
        }

        order.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.OrderRepository.Update(order);
        if (request.Status != null && order.Status != oldStatus)
        {
            await _orderNotificationPublisher.PublishOrderStatusChangedAsync(
                order.OrderId,
                order.UserId,
                order.OrderCode,
                order.Status,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await TryRecordPromotionUsageAsync(order, cancellationToken);
    }

    public Task UpdateStatusAsync(Guid orderId, OrderState status, CancellationToken cancellationToken = default)
        => UpdateStatusAsync(orderId, status, null, cancellationToken);

    public async Task UpdateStatusAsync(
        Guid orderId,
        OrderState status,
        string? statusNote,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.OrderRepository.GetByOrderIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order not found: {orderId}");
        var oldStatus = order.Status;
        var now = DateTime.UtcNow;

        if (oldStatus == status)
            return;

        if (status == OrderState.Canceled)
        {
            if (string.IsNullOrWhiteSpace(statusNote))
                throw new InvalidOperationException("Vui lòng nhập lý do hủy đơn hàng.");

            if (order.Status == OrderState.Pending)
            {
                // Allowed to cancel freely.
            }
            else if (order.Status == OrderState.Paid)
            {
                if (!order.CancelDeadline.HasValue)
                    throw new InvalidOperationException("Đơn hàng đã thanh toán nhưng chưa có hạn hủy (CancelDeadline). Không thể hủy.");

                if (now > order.CancelDeadline.Value)
                    throw new InvalidOperationException("Đã quá thời gian cho phép hủy đơn sau khi thanh toán.");
            }
            else
            {
                throw new InvalidOperationException($"Không thể hủy đơn ở trạng thái {order.Status}.");
            }
        }

        if (status == OrderState.Paid && !order.CancelDeadline.HasValue)
        {
            var windowMinutes = await GetCancelWindowMinutesAfterPaidAsync(cancellationToken);
            order.CancelDeadline = now.AddMinutes(windowMinutes);
        }

        if (status == OrderState.Canceled && oldStatus == OrderState.Paid)
            await RestoreStockForOrderAsync(orderId, now, cancellationToken);

        // RTS -> Refunded: hoàn kho + detach khỏi DeliveryGroup (tương tự luồng hủy sau Paid).
        if (status == OrderState.Refunded && oldStatus == OrderState.ReadyToShip)
        {
            await RestoreStockForOrderAsync(orderId, now, cancellationToken);
            await DetachOrderFromDeliveryGroupIfNeededAsync(order, now, cancellationToken);
        }

        order.Status = status;
        order.UpdatedAt = now;
        _unitOfWork.OrderRepository.Update(order);
        var normalizedNote = string.IsNullOrWhiteSpace(statusNote) ? null : statusNote.Trim();
        var logNote = status switch
        {
            OrderState.Canceled => normalizedNote,
            OrderState.Refunded => normalizedNote,
            _ => null
        };
        await TryCreateStatusLogAsync(order.OrderId, oldStatus, status, "system", logNote, cancellationToken);
        await _orderNotificationPublisher.PublishOrderStatusChangedAsync(
            order.OrderId,
            order.UserId,
            order.OrderCode,
            status,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await TryRecordPromotionUsageAsync(order, cancellationToken);
    }

    public async Task<OrderResponseDto> CreateForCustomerAsync(Guid userId, CreateOwnOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        var customer = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản.");
        if (customer.Status != UserState.Active)
            throw new InvalidOperationException("Tài khoản không ở trạng thái hoạt động, không thể đặt hàng.");

        var totalAmount = request.OrderItems.Sum(x => x.UnitPrice * x.Quantity);
        var model = new CreateOrderRequestDto
        {
            UserId = userId,
            TimeSlotId = request.TimeSlotId,
            CollectionId = request.CollectionId,
            DeliveryType = request.DeliveryType,
            TotalAmount = totalAmount,
            DiscountAmount = 0m,
            FinalAmount = 0m,
            DeliveryFee = request.DeliveryFee,
            Status = OrderState.Pending.ToString(),
            AddressId = request.AddressId,
            PromotionId = request.PromotionId,
            DeliveryNote = request.DeliveryNote,
            CancelDeadline = request.CancelDeadline,
            DeliveryDate = request.DeliveryDate,
            OrderItems = request.OrderItems
        };

        return await CreateAsync(model, cancellationToken);
    }

    private static DateTime ResolveRequestedDeliveryDate(DateTime? deliveryDate, DateTime orderPlacedAtUtc)
    {
        if (!deliveryDate.HasValue)
            return orderPlacedAtUtc;

        var requested = deliveryDate.Value;
        if (requested.Kind == DateTimeKind.Unspecified)
            requested = DateTime.SpecifyKind(requested, DateTimeKind.Utc);
        else if (requested.Kind == DateTimeKind.Local)
            requested = requested.ToUniversalTime();

        var requestedDate = requested.Date;
        var todayUtc = orderPlacedAtUtc.Date;
        const int maxDaysAhead = 6;

        if (requestedDate < todayUtc)
            throw new InvalidOperationException("Ngày giao / nhận không được ở quá khứ.");

        if (requestedDate > todayUtc.AddDays(maxDaysAhead))
            throw new InvalidOperationException("Chỉ có thể đặt giao / nhận trong vòng 1 tuần (7 ngày).");

        return DateTime.SpecifyKind(requestedDate, DateTimeKind.Utc);
    }

    public async Task<(IEnumerable<OrderResponseDto> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = await _unitOfWork.OrderRepository.GetAllAsync(cancellationToken);
        var list = all.Where(x => x.UserId == userId).OrderByDescending(x => x.OrderDate).ToList();
        var total = list.Count;
        var items = list.Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(x => _mapper.Map<OrderResponseDto>(x)).ToList();
        return (items, total);
    }

    public async Task<OrderResponseDto> ApplyPromotionAsync(Guid orderId, Guid userId, ApplyPromotionToOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order not found: {orderId}");

        if (order.UserId != userId)
            throw new InvalidOperationException("Bạn không có quyền áp dụng khuyến mãi cho đơn này");

        var validation = await _promotionService.ValidatePromotionAsync(
            userId,
            new ValidatePromotionRequestDto
            {
                PromotionId = request.PromotionId,
                PromotionCode = request.PromotionCode,
                TotalAmount = order.TotalAmount
            },
            cancellationToken);
        if (!validation.IsValid || !validation.PromotionId.HasValue)
            throw new InvalidOperationException(validation.Message);

        order.PromotionId = validation.PromotionId.Value;
        order.DiscountAmount = validation.DiscountAmount;
        order.SystemUsageFeeAmount = await GetOrderSystemUsageFeeVndAsync(cancellationToken);
        order.FinalAmount = OrderTotalsHelper.ComputeFinalAmount(
            order.TotalAmount,
            order.DiscountAmount,
            order.DeliveryFee,
            order.SystemUsageFeeAmount);
        order.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.OrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await TryRecordPromotionUsageAsync(order, cancellationToken);

        var updated = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken);
        return _mapper.Map<OrderResponseDto>(updated!);
    }

    private async Task ValidateOrderLotsForCreationAsync(
        IReadOnlyCollection<CreateOrderItemDto> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            throw new InvalidOperationException("Đơn hàng phải có ít nhất một sản phẩm.");

        var lotIds = items.Select(x => x.LotId).Distinct().ToList();
        var lots = (await _unitOfWork.Repository<StockLot>().FindAsync(l => lotIds.Contains(l.LotId)))
            .ToDictionary(l => l.LotId);

        var requiredByLot = await _purchaseUnitHelper.SumRequiredQuantitiesInLotUnitAsync(
            items,
            lots,
            cancellationToken);

        var now = DateTime.UtcNow;
        var cutoffReached = DailyExpiryOrderingPolicy.IsOrderCutoffReached(now);
        foreach (var (lotId, requiredQuantity) in requiredByLot)
        {
            if (!lots.TryGetValue(lotId, out var lot))
                throw new InvalidOperationException($"Không tìm thấy StockLot {lotId}.");

            if (lot.Status != ProductState.Published)
                throw new InvalidOperationException($"StockLot {lotId} không còn ở trạng thái Published.");

            if (lot.ExpiryDate <= now)
                throw new InvalidOperationException($"StockLot {lotId} đã hết hạn, không thể đặt.");

            if (cutoffReached && DailyExpiryOrderingPolicy.IsExpiringInVietnamToday(lot.ExpiryDate, now))
                throw new InvalidOperationException(
                    "Sau 21:00, không thể đặt lô hàng có hạn sử dụng trong ngày.");

            if (lot.Quantity < requiredQuantity)
                throw new InvalidOperationException(
                    $"StockLot {lotId} không đủ số lượng. Cần {requiredQuantity} (đơn vị lô), còn {lot.Quantity}.");
        }
    }

    public async Task DeleteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.OrderRepository.GetByOrderIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order not found: {orderId}");
        _unitOfWork.OrderRepository.Delete(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task TryRecordPromotionUsageAsync(Order order, CancellationToken cancellationToken)
    {
        if (!order.PromotionId.HasValue || order.DiscountAmount <= 0)
            return;

        if (order.Status is not (OrderState.Paid or OrderState.ReadyToShip or OrderState.DeliveredWaitConfirm or OrderState.Completed))
            return;

        await _promotionUsageService.RecordUsageAsync(order.PromotionId.Value, order.UserId, order.OrderId, order.DiscountAmount, cancellationToken);
    }

    private async Task RestoreStockForOrderAsync(Guid orderId, DateTime now, CancellationToken cancellationToken)
    {
        var orderItems = (await _unitOfWork.Repository<OrderItem>().FindAsync(oi => oi.OrderId == orderId)).ToList();
        if (orderItems.Count == 0)
            return;

        var requiredByLot = await _orderStockQuantityHelper.ComputeRequiredStockQuantityByLotAsync(
            orderItems,
            cancellationToken);

        var lotIds = requiredByLot.Keys.ToList();
        var lots = await _unitOfWork.Repository<StockLot>().FindAsync(l => lotIds.Contains(l.LotId));
        var lotById = lots.ToDictionary(l => l.LotId);

        foreach (var (lotId, requiredQuantity) in requiredByLot)
        {
            if (!lotById.TryGetValue(lotId, out var lot))
                throw new InvalidOperationException($"Không tìm thấy StockLot {lotId} để hoàn kho cho order {orderId}.");

            lot.Quantity += requiredQuantity;
            lot.UpdatedAt = now;
            _unitOfWork.Repository<StockLot>().Update(lot);
        }
    }

    private async Task TryAutoAssignDeliveryGroupAsync(Order order, CancellationToken cancellationToken)
    {
        // Auto-group only for pickup orders by (TimeSlot + CollectionPoint + DeliveryDate).
        // Home-delivery orders should be grouped by delivery planner/dispatch flow instead.
        if (!order.CollectionId.HasValue)
            return;

        // Only auto-group paid orders in active fulfillment flow.
        // Pending (unpaid) orders are excluded to avoid reserving delivery capacity too early.
        if (order.Status is not (OrderState.Paid or OrderState.ReadyToShip or OrderState.DeliveredWaitConfirm))
            return;

        var deliveryArea = $"COLLECTION:{order.CollectionId.Value}";
        var existing = await _unitOfWork.Repository<DeliveryGroup>().FirstOrDefaultAsync(g =>
            g.TimeSlotId == order.TimeSlotId
            && g.DeliveryDate.Date == order.OrderDate.Date
            && g.DeliveryArea == deliveryArea
            && g.Status != DeliveryGroupState.Completed);

        if (existing == null)
        {
            existing = new DeliveryGroup
            {
                DeliveryGroupId = Guid.NewGuid(),
                GroupCode = "DG-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
                TimeSlotId = order.TimeSlotId,
                DeliveryType = DeliveryMethod.Pickup,
                DeliveryArea = deliveryArea,
                DeliveryDate = order.OrderDate.Date,
                Status = DeliveryGroupState.Pending,
                TotalOrders = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<DeliveryGroup>().AddAsync(existing);
        }

        existing.TotalOrders += 1;
        existing.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<DeliveryGroup>().Update(existing);
        order.DeliveryGroupId = existing.DeliveryGroupId;
    }

    private async Task DetachOrderFromDeliveryGroupIfNeededAsync(Order order, DateTime now, CancellationToken cancellationToken)
    {
        if (!order.DeliveryGroupId.HasValue)
            return;

        var groupId = order.DeliveryGroupId.Value;
        var group = await _unitOfWork.Repository<DeliveryGroup>()
            .FirstOrDefaultAsync(g => g.DeliveryGroupId == groupId);

        if (group != null && group.TotalOrders > 0)
        {
            group.TotalOrders -= 1;
            group.UpdatedAt = now;
            _unitOfWork.Repository<DeliveryGroup>().Update(group);
        }

        order.DeliveryGroupId = null;
    }

    private async Task TryCreateStatusLogAsync(Guid orderId, OrderState from, OrderState to, string? changedBy, string? note, CancellationToken cancellationToken)
    {
        var log = new OrderStatusLog
        {
            LogId = Guid.NewGuid(),
            OrderId = orderId,
            FromStatus = from,
            ToStatus = to,
            ChangedBy = changedBy,
            Note = note,
            ChangedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<OrderStatusLog>().AddAsync(log);
    }

    private async Task<int> GetCancelWindowMinutesAfterPaidAsync(CancellationToken cancellationToken)
    {
        var config = await _unitOfWork.Repository<SystemConfig>()
            .FirstOrDefaultAsync(x => x.ConfigKey == SystemConfigKeys.OrderCancelWindowMinutesAfterPaid);

        if (config == null)
            throw new InvalidOperationException(
                $"Thiếu SystemConfig '{SystemConfigKeys.OrderCancelWindowMinutesAfterPaid}'. Vui lòng cấu hình số phút cho phép hủy sau khi thanh toán.");

        if (!int.TryParse(config.ConfigValue, out var minutes) || minutes <= 0)
            throw new InvalidOperationException(
                $"SystemConfig '{SystemConfigKeys.OrderCancelWindowMinutesAfterPaid}' không hợp lệ. Giá trị phải là số nguyên dương.");

        return minutes;
    }

    private async Task<decimal> GetOrderSystemUsageFeeVndAsync(CancellationToken cancellationToken)
    {
        var config = await _unitOfWork.Repository<SystemConfig>()
            .FirstOrDefaultAsync(x => x.ConfigKey == SystemConfigKeys.OrderSystemUsageFeeVnd);

        if (config == null)
            throw new InvalidOperationException(
                $"Thiếu SystemConfig '{SystemConfigKeys.OrderSystemUsageFeeVnd}'. Vui lòng cấu hình phí sử dụng hệ thống (VND mỗi đơn).");

        if (!decimal.TryParse(
                config.ConfigValue,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var fee)
            || fee < 0)
            throw new InvalidOperationException(
                $"SystemConfig '{SystemConfigKeys.OrderSystemUsageFeeVnd}' không hợp lệ. Giá trị phải là số không âm.");

        return fee;
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
}








