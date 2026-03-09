using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class OrderItemService : IOrderItemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public OrderItemService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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
        var orderItem = new OrderItem
        {
            OrderItemId = Guid.NewGuid(),
            OrderId = request.OrderId,
            LotId = request.LotId,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice
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

        if (request.LotId.HasValue) item.LotId = request.LotId.Value;
        if (request.Quantity.HasValue) item.Quantity = request.Quantity.Value;
        if (request.UnitPrice.HasValue) item.UnitPrice = request.UnitPrice.Value;

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
