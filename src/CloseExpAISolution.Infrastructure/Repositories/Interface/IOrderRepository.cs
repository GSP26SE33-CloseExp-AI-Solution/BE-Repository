using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Infrastructure.Repositories.Interface;

public interface IOrderRepository
{
    Task<Order?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdWithDetailsAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetAdminPagedAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        string? status,
        string? deliveryType,
        Guid? userId,
        Guid? timeSlotId,
        Guid? collectionId,
        Guid? deliveryGroupId,
        string? search,
        string sortBy,
        string sortDir,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    void Update(Order order);
    void Delete(Order order);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
