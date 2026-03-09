using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CloseExpAISolution.Infrastructure.Repositories.Class;

public class OrderItemRepository : IOrderItemRepository
{
    private readonly ApplicationDbContext _context;

    public OrderItemRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrderItem?> GetByOrderItemIdAsync(Guid orderItemId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderItems
            .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId, cancellationToken);
    }

    public async Task<OrderItem?> GetByIdWithDetailsAsync(Guid orderItemId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.ProductLot)
                .ThenInclude(pl => pl!.Product)
            .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId, cancellationToken);
    }

    public async Task<IEnumerable<OrderItem>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderItems
            .Include(oi => oi.ProductLot)
                .ThenInclude(pl => pl!.Product)
            .Where(oi => oi.OrderId == orderId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OrderItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.ProductLot)
                .ThenInclude(pl => pl!.Product)
            .OrderBy(oi => oi.OrderId)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderItem> AddAsync(OrderItem orderItem, CancellationToken cancellationToken = default)
    {
        await _context.OrderItems.AddAsync(orderItem, cancellationToken);
        return orderItem;
    }

    public void Update(OrderItem orderItem)
    {
        _context.OrderItems.Update(orderItem);
    }

    public void Delete(OrderItem orderItem)
    {
        _context.OrderItems.Remove(orderItem);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OrderItems.CountAsync(cancellationToken);
    }

    public async Task<int> CountByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderItems.CountAsync(oi => oi.OrderId == orderId, cancellationToken);
    }
}
