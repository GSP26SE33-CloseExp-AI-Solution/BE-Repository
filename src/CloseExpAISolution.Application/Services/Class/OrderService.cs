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

        return slots
            .OrderBy(x => x.StartTime)
            .Select(x => new DeliveryTimeSlotDto
            {
                TimeSlotId = x.TimeSlotId,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                DisplayTimeRange = $"{x.StartTime:hh\\:mm} - {x.EndTime:hh\\:mm}"
            })
            .ToList();
    }

    public async Task<IEnumerable<PickupPointDto>> GetCollectionPointsAsync(CancellationToken cancellationToken = default)
    {
        var points = await _unitOfWork.Repository<CollectionPoint>().GetAllAsync();

        return points
            .OrderBy(x => x.Name)
            .Select(x => new PickupPointDto
            {
                PickupPointId = x.CollectionId,
                Name = x.Name,
                Address = x.AddressLine
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
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.OrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await TryRecordPromotionUsageAsync(order, cancellationToken);
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
}








