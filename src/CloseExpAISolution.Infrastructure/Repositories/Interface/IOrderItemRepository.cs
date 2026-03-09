using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Infrastructure.Repositories.Interface;

public interface IOrderItemRepository
{
    Task<OrderItem?> GetByOrderItemIdAsync(Guid orderItemId, CancellationToken cancellationToken = default);
    Task<OrderItem?> GetByIdWithDetailsAsync(Guid orderItemId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderItem>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrderItem> AddAsync(OrderItem orderItem, CancellationToken cancellationToken = default);
    void Update(OrderItem orderItem);
    void Delete(OrderItem orderItem);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
