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

    public OrderService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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
            PickupPointId = request.PickupPointId,
            DeliveryType = request.DeliveryType,
            TotalAmount = request.TotalAmount,
            DiscountAmount = request.DiscountAmount,
            FinalAmount = request.FinalAmount,
            DeliveryFee = request.DeliveryFee,
            Status = request.Status,
            OrderDate = DateTime.UtcNow,
            AddressId = request.AddressId,
            PromotionId = request.PromotionId,
            DeliveryGroupId = request.DeliveryGroupId,
            DeliveryAddress = request.DeliveryAddress,
            DeliveryNote = request.DeliveryNote,
            CancelDeadline = request.CancelDeadline,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var item in request.OrderItems)
        {
            var totalPrice = item.Quantity * item.UnitPrice;
            order.OrderItems.Add(new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = orderId,
                LotId = item.LotId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = totalPrice
            });
        }

        await _unitOfWork.OrderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken);
        return _mapper.Map<OrderResponseDto>(created!);
    }

    public async Task UpdateAsync(Guid orderId, UpdateOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order not found: {orderId}");

        if (request.TimeSlotId.HasValue) order.TimeSlotId = request.TimeSlotId.Value;
        if (request.PickupPointId.HasValue) order.PickupPointId = request.PickupPointId;
        if (request.DeliveryType != null) order.DeliveryType = request.DeliveryType;
        if (request.TotalAmount.HasValue) order.TotalAmount = request.TotalAmount.Value;
        if (request.Status != null) order.Status = request.Status;
        if (request.AddressId.HasValue) order.AddressId = request.AddressId.Value;
        if (request.PromotionId.HasValue) order.PromotionId = request.PromotionId;
        if (request.DeliveryGroupId.HasValue) order.DeliveryGroupId = request.DeliveryGroupId;
        if (request.DeliveryAddress != null) order.DeliveryAddress = request.DeliveryAddress;
        if (request.DeliveryNote != null) order.DeliveryNote = request.DeliveryNote;
        if (request.DiscountAmount.HasValue) order.DiscountAmount = request.DiscountAmount.Value;
        if (request.FinalAmount.HasValue) order.FinalAmount = request.FinalAmount.Value;
        if (request.DeliveryFee.HasValue) order.DeliveryFee = request.DeliveryFee.Value;
        if (request.CancelDeadline.HasValue) order.CancelDeadline = request.CancelDeadline;

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
                    Quantity = dto.Quantity,
                    UnitPrice = dto.UnitPrice,
                    TotalPrice = totalPrice
                });
            }
        }

        order.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.OrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid orderId, OrderState status, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.OrderRepository.GetByOrderIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order not found: {orderId}");
        order.Status = status.ToString();
        order.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.OrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.OrderRepository.GetByOrderIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order not found: {orderId}");
        _unitOfWork.OrderRepository.Delete(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
