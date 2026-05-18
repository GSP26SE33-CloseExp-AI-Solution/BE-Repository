using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class OrderItemService : IOrderItemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly OrderItemUnitConverter _orderItemUnitConverter;

    public OrderItemService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        OrderItemUnitConverter orderItemUnitConverter)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _orderItemUnitConverter = orderItemUnitConverter;
    }

    public async Task<(IEnumerable<OrderItemResponseDto> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, Guid? orderId = null, CancellationToken cancellationToken = default)
    {
        var query = orderId.HasValue
            ? await _unitOfWork.OrderItemRepository.GetByOrderIdAsync(orderId.Value, cancellationToken)
            : await _unitOfWork.OrderItemRepository.GetAllAsync(cancellationToken);
        var list = query.ToList();
        var total = list.Count;
        var items = list
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(oi => _mapper.Map<OrderItemResponseDto>(oi))
            .ToList();
        return (items, total);
    }

    public async Task<OrderItemResponseDto?> GetByIdAsync(Guid orderItemId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.OrderItemRepository.GetByOrderItemIdAsync(orderItemId, cancellationToken);
        return item == null ? null : _mapper.Map<OrderItemResponseDto>(item);
    }

    public async Task<OrderItemResponseDto?> GetByIdWithDetailsAsync(Guid orderItemId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.OrderItemRepository.GetByIdWithDetailsAsync(orderItemId, cancellationToken);
        return item == null ? null : _mapper.Map<OrderItemResponseDto>(item);
    }

    public async Task<IEnumerable<OrderItemResponseDto>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.OrderItemRepository.GetByOrderIdAsync(orderId, cancellationToken);
        return _mapper.Map<IEnumerable<OrderItemResponseDto>>(items);
    }

    public async Task<OrderItemResponseDto> CreateAsync(CreateOrderItemRequestDto request, CancellationToken cancellationToken = default)
    {
        var converted = await _orderItemUnitConverter.ConvertCreateItemsToProductUnitAsync(
            new[]
            {
                new CreateOrderItemDto
                {
                    LotId = request.LotId,
                    PurchaseUnitId = request.PurchaseUnitId,
                    Quantity = request.Quantity,
                    UnitPrice = request.UnitPrice
                }
            },
            cancellationToken);

        var line = converted[0];
        var orderItem = new OrderItem
        {
            OrderItemId = Guid.NewGuid(),
            OrderId = request.OrderId,
            LotId = line.LotId,
            PurchaseUnitId = line.PurchaseUnitId,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            TotalPrice = line.Quantity * line.UnitPrice
        };

        await _unitOfWork.OrderItemRepository.AddAsync(orderItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _unitOfWork.OrderItemRepository.GetByIdWithDetailsAsync(orderItem.OrderItemId, cancellationToken);
        return _mapper.Map<OrderItemResponseDto>(created!);
    }

    public async Task UpdateAsync(Guid orderItemId, UpdateOrderItemRequestDto request, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.OrderItemRepository.GetByOrderItemIdAsync(orderItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order item not found: {orderItemId}");

        var hasUnitChange = request.PurchaseUnitId.HasValue
            || request.LotId.HasValue
            || request.Quantity.HasValue
            || request.UnitPrice.HasValue;

        if (hasUnitChange)
        {
            var converted = await _orderItemUnitConverter.ConvertUpdateItemToProductUnitAsync(
                item,
                request,
                cancellationToken);

            item.LotId = converted.LotId;
            item.PurchaseUnitId = converted.PurchaseUnitId;
            item.Quantity = converted.Quantity;
            item.UnitPrice = converted.UnitPrice;
            item.TotalPrice = converted.Quantity * converted.UnitPrice;
        }

        _unitOfWork.OrderItemRepository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid orderItemId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.OrderItemRepository.GetByOrderItemIdAsync(orderItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order item not found: {orderItemId}");
        _unitOfWork.OrderItemRepository.Delete(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
