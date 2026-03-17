using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Infrastructure.Repositories.Interface;

public interface IOrderRepository
{
    Task<Order?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdWithDetailsAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    void Update(Order order);
    void Delete(Order order);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
