using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPromotionService _promotionService;
    private readonly IPromotionUsageService _promotionUsageService;

    public OrderService(IUnitOfWork unitOfWork, IMapper mapper, IPromotionService promotionService, IPromotionUsageService promotionUsageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _promotionService = promotionService;
        _promotionUsageService = promotionUsageService;
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

    public async Task<IEnumerable<PickupPointDto>> GetCollectionPointsAsync(CancellationToken cancellationToken = default)
    {
        var points = await _unitOfWork.Repository<CollectionPoint>().GetAllAsync();
        var orderCountByCollection = await GetOrderCollectionCountsAsync(cancellationToken);

        return points
            .OrderBy(x => x.Name)
            .Select(x => new PickupPointDto
            {
                PickupPointId = x.CollectionId,
                Name = x.Name,
                Address = x.AddressLine,
                RelatedOrderCount = orderCountByCollection.TryGetValue(x.CollectionId, out var c) ? c : 0
            })
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
        var orderId = Guid.NewGuid();
        var orderCode = "ORD-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var order = new Order
        {
            OrderId = orderId,
            OrderCode = orderCode,
            UserId = request.UserId,
            TimeSlotId = request.TimeSlotId,
            CollectionId = request.CollectionId,
            DeliveryType = request.DeliveryType,
            TotalAmount = request.TotalAmount,
            DiscountAmount = request.DiscountAmount,
            FinalAmount = request.FinalAmount,
            DeliveryFee = request.DeliveryFee,
            Status = Enum.Parse<OrderState>(request.Status),
            OrderDate = DateTime.UtcNow,
            AddressId = request.AddressId,
            PromotionId = request.PromotionId,
            DeliveryGroupId = request.DeliveryGroupId,
            DeliveryNote = request.DeliveryNote,
            CancelDeadline = request.CancelDeadline,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
            order.FinalAmount = validation.FinalAmount;
        }

        foreach (var item in request.OrderItems)
        {
            var totalPrice = item.Quantity * item.UnitPrice;
            order.OrderItems.Add(new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = orderId,
                LotId = item.LotId,
                Quantity = (short)item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = totalPrice
            });
        }

        await _unitOfWork.OrderRepository.AddAsync(order, cancellationToken);
        await TryAutoAssignDeliveryGroupAsync(order, cancellationToken);
        await TryCreateNotificationAsync(order.UserId, "Đơn hàng mới", $"Đơn {order.OrderCode} đã được tạo thành công.", NotificationType.OrderUpdate, cancellationToken);
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

        if (request.TimeSlotId.HasValue) order.TimeSlotId = request.TimeSlotId.Value;
        if (request.CollectionId.HasValue) order.CollectionId = request.CollectionId;
        if (request.DeliveryType != null) order.DeliveryType = request.DeliveryType;
        if (request.TotalAmount.HasValue) order.TotalAmount = request.TotalAmount.Value;
        if (request.Status != null) order.Status = Enum.Parse<OrderState>(request.Status);
        if (request.AddressId.HasValue) order.AddressId = request.AddressId;
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
            order.FinalAmount = validation.FinalAmount;
        }

        if (request.OrderItems != null && request.OrderItems.Count > 0)
        {
            order.OrderItems.Clear();
            foreach (var dto in request.OrderItems)
            {
                var totalPrice = dto.Quantity * dto.UnitPrice;
                order.OrderItems.Add(new OrderItem
                {
                    OrderItemId = dto.OrderItemId == Guid.Empty ? Guid.NewGuid() : dto.OrderItemId,
                    OrderId = orderId,
                    LotId = dto.LotId,
                    Quantity = (short)dto.Quantity,
                    UnitPrice = dto.UnitPrice,
                    TotalPrice = totalPrice
                });
            }
        }

        order.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.OrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await TryRecordPromotionUsageAsync(order, cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid orderId, OrderState status, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.OrderRepository.GetByOrderIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order not found: {orderId}");
        var oldStatus = order.Status;
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.OrderRepository.Update(order);
        await TryCreateStatusLogAsync(order.OrderId, oldStatus, status, "system", null, cancellationToken);
        await TryCreateNotificationAsync(order.UserId, "Cập nhật đơn hàng", $"Đơn {order.OrderCode} đã chuyển sang trạng thái {status}.", NotificationType.OrderUpdate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await TryRecordPromotionUsageAsync(order, cancellationToken);
    }

    public async Task<OrderResponseDto> CreateForCustomerAsync(Guid userId, CreateOwnOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        var totalAmount = request.OrderItems.Sum(x => x.UnitPrice * x.Quantity);
        var model = new CreateOrderRequestDto
        {
            UserId = userId,
            TimeSlotId = request.TimeSlotId,
            CollectionId = request.CollectionId,
            DeliveryType = request.DeliveryType,
            TotalAmount = totalAmount,
            DiscountAmount = 0m,
            FinalAmount = totalAmount + request.DeliveryFee,
            DeliveryFee = request.DeliveryFee,
            Status = OrderState.Pending.ToString(),
            AddressId = request.AddressId,
            PromotionId = request.PromotionId,
            DeliveryNote = request.DeliveryNote,
            CancelDeadline = request.CancelDeadline,
            OrderItems = request.OrderItems
        };

        return await CreateAsync(model, cancellationToken);
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
        order.FinalAmount = validation.FinalAmount;
        order.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.OrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await TryRecordPromotionUsageAsync(order, cancellationToken);

        var updated = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken);
        return _mapper.Map<OrderResponseDto>(updated!);
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

        if (order.Status is not (OrderState.PaidProcessing or OrderState.ReadyToShip or OrderState.DeliveredWaitConfirm or OrderState.Completed))
            return;

        await _promotionUsageService.RecordUsageAsync(order.PromotionId.Value, order.UserId, order.OrderId, order.DiscountAmount, cancellationToken);
    }

    private async Task TryAutoAssignDeliveryGroupAsync(Order order, CancellationToken cancellationToken)
    {
        // Auto-group only for pickup orders by (TimeSlot + CollectionPoint + DeliveryDate).
        // Home-delivery orders should be grouped by delivery planner/dispatch flow instead.
        if (!order.CollectionId.HasValue)
            return;

        // Only auto-group paid orders in active fulfillment flow.
        // Pending (unpaid) orders are excluded to avoid reserving delivery capacity too early.
        if (order.Status is not (OrderState.PaidProcessing or OrderState.ReadyToShip or OrderState.DeliveredWaitConfirm))
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
                DeliveryType = "Pickup",
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

    private async Task TryCreateNotificationAsync(Guid userId, string title, string content, NotificationType type, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Content = content,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<Notification>().AddAsync(notification);
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








