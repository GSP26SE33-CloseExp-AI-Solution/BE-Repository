using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

public sealed class StockLotUnitCompatibilityService : IStockLotUnitCompatibilityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StockLotUnitCompatibilityService> _logger;

    public StockLotUnitCompatibilityService(
        ApplicationDbContext context,
        ILogger<StockLotUnitCompatibilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StockLotUnitCleanupResult> RemoveIncompatibleStockLotsAsync(
        CancellationToken cancellationToken = default)
    {
        var lots = await _context.StockLots
            .Include(l => l.Unit)
            .Include(l => l.Product)
                .ThenInclude(p => p!.Unit)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var incompatible = lots
            .Where(l => l.Product?.Unit != null
                        && l.Unit != null
                        && !UnitMeasureTypeCompatibility.AreCompatible(
                            l.Unit.Type,
                            l.Product.Unit.Type))
            .ToList();

        if (incompatible.Count == 0)
        {
            return new StockLotUnitCleanupResult();
        }

        var incompatibleIds = incompatible.Select(l => l.LotId).ToList();
        var lotIdsWithOrders = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => incompatibleIds.Contains(oi.LotId))
            .Select(oi => oi.LotId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var withOrders = lotIdsWithOrders.ToHashSet();
        var deleted = 0;
        var archived = 0;
        var now = DateTime.UtcNow;

        foreach (var lot in incompatible)
        {
            if (withOrders.Contains(lot.LotId))
            {
                lot.Status = ProductState.Deleted;
                lot.UpdatedAt = now;
                archived++;
                _logger.LogWarning(
                    "Archived incompatible StockLot {LotId} (has order lines). Product={ProductId}, lot unit type={LotType}, product unit type={ProductType}",
                    lot.LotId,
                    lot.ProductId,
                    lot.Unit?.Type,
                    lot.Product?.Unit?.Type);
            }
            else
            {
                _context.StockLots.Remove(lot);
                deleted++;
                _logger.LogWarning(
                    "Deleted incompatible StockLot {LotId}. Product={ProductId}, lot unit type={LotType}, product unit type={ProductType}",
                    lot.LotId,
                    lot.ProductId,
                    lot.Unit?.Type,
                    lot.Product?.Unit?.Type);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new StockLotUnitCleanupResult
        {
            DeletedCount = deleted,
            ArchivedCount = archived,
            TotalIncompatible = incompatible.Count,
        };
    }
}
