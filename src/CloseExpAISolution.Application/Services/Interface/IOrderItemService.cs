using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IOrderItemService
{
    Task<(IEnumerable<OrderItemResponseDto> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, Guid? orderId = null, CancellationToken cancellationToken = default);
    Task<OrderItemResponseDto?> GetByIdAsync(Guid orderItemId, CancellationToken cancellationToken = default);
    Task<OrderItemResponseDto?> GetByIdWithDetailsAsync(Guid orderItemId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderItemResponseDto>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderItemResponseDto> CreateAsync(CreateOrderItemRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid orderItemId, UpdateOrderItemRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid orderItemId, CancellationToken cancellationToken = default);
}
