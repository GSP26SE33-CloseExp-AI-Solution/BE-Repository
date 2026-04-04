using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CloseExpAISolution.Infrastructure.Repositories.Class;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
    }

    public async Task<Order?> GetByIdWithDetailsAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.User)
            .Include(o => o.DeliveryTimeSlot)
            .Include(o => o.CollectionPoint)
            .Include(o => o.Promotion)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.StockLot)
                    .ThenInclude(pl => pl!.Product)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.User)
            .Include(o => o.DeliveryTimeSlot)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetAdminPagedAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        string? status,
        string? deliveryType,
        Guid? userId,
        Guid? timeSlotId,
        Guid? collectionId,
        Guid? deliveryGroupId,
        bool unassignedOnly,
        string? search,
        string sortBy,
        string sortDir,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, pageNumber);
        var safeSize = Math.Clamp(pageSize, 1, 200);

        IQueryable<Order> q = _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.DeliveryTimeSlot)
            .Include(o => o.CollectionPoint);

        if (fromUtc.HasValue) q = q.Where(o => o.OrderDate >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(o => o.OrderDate <= toUtc.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim();
            if (Enum.TryParse<CloseExpAISolution.Domain.Enums.OrderState>(normalized, ignoreCase: true, out var orderState))
            {
                // Compare enum directly so EF can translate to SQL.
                q = q.Where(o => o.Status == orderState);
            }
        }

        if (!string.IsNullOrWhiteSpace(deliveryType))
        {
            var normalized = deliveryType.Trim();
            q = q.Where(o => o.DeliveryType == normalized);
        }

        if (userId.HasValue) q = q.Where(o => o.UserId == userId.Value);
        if (timeSlotId.HasValue) q = q.Where(o => o.TimeSlotId == timeSlotId.Value);
        if (collectionId.HasValue) q = q.Where(o => o.CollectionId == collectionId.Value);
        if (unassignedOnly)
            q = q.Where(o => o.DeliveryGroupId == null);
        else if (deliveryGroupId.HasValue)
            q = q.Where(o => o.DeliveryGroupId == deliveryGroupId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(o => o.OrderCode.Contains(s));
        }

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var by = (sortBy ?? string.Empty).Trim();

        q = by.ToLowerInvariant() switch
        {
            "finalamount" => desc ? q.OrderByDescending(o => o.FinalAmount) : q.OrderBy(o => o.FinalAmount),
            "status" => desc ? q.OrderByDescending(o => o.Status) : q.OrderBy(o => o.Status),
            "createdat" => desc ? q.OrderByDescending(o => o.CreatedAt) : q.OrderBy(o => o.CreatedAt),
            "updatedat" => desc ? q.OrderByDescending(o => o.UpdatedAt) : q.OrderBy(o => o.UpdatedAt),
            "ordercode" => desc ? q.OrderByDescending(o => o.OrderCode) : q.OrderBy(o => o.OrderCode),
            _ => desc ? q.OrderByDescending(o => o.OrderDate) : q.OrderBy(o => o.OrderDate)
        };

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
        return order;
    }

    public void Update(Order order)
    {
        _context.Orders.Update(order);
    }

    public void Delete(Order order)
    {
        _context.Orders.Remove(order);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders.CountAsync(cancellationToken);
    }
}
