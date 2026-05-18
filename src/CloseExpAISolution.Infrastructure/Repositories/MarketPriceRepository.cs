using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CloseExpAISolution.Infrastructure.Repositories;

public class MarketPriceRepository : IMarketPriceRepository
{
    private readonly ApplicationDbContext _context;

    public MarketPriceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MarketPrice>> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _context.MarketPrices
            .Where(mp => mp.Barcode == barcode && mp.Status == MarketPriceState.Active)
            .OrderBy(mp => mp.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task<MarketPrice?> GetMinPriceByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _context.MarketPrices
            .Where(mp => mp.Barcode == barcode && mp.Status == MarketPriceState.Active && mp.IsInStock)
            .OrderBy(mp => mp.Price)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<MarketPrice>> SearchByProductNameAsync(string productName, CancellationToken cancellationToken = default)
    {
        var searchTerms = productName.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return await _context.MarketPrices
            .Where(mp => mp.Status == MarketPriceState.Active &&
                         mp.ProductName != null &&
                         searchTerms.All(term => mp.ProductName.ToLower().Contains(term)))
            .OrderBy(mp => mp.Price)
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    public async Task<MarketPrice> InsertObservationAsync(MarketPrice marketPrice, DateTime? batchTimestamp = null, CancellationToken cancellationToken = default)
    {
        marketPrice.MarketPriceId = Guid.NewGuid();
        marketPrice.CollectedAt = batchTimestamp ?? DateTime.UtcNow;
        marketPrice.LastUpdated = marketPrice.CollectedAt;
        await _context.MarketPrices.AddAsync(marketPrice, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return marketPrice;
    }

    public async Task BulkInsertObservationsAsync(IEnumerable<MarketPrice> marketPrices, DateTime? batchTimestamp = null, CancellationToken cancellationToken = default)
    {
        var collectedAt = batchTimestamp ?? DateTime.UtcNow;
        // Dedupe within one crawl batch to prevent duplicated observations.
        var uniquePrices = marketPrices
            .GroupBy(p => new
            {
                Barcode = p.Barcode.Trim(),
                Source = p.Source.Trim(),
                Store = (p.StoreName ?? string.Empty).Trim().ToLowerInvariant()
            })
            .Select(g => g.First())
            .ToList();

        foreach (var price in uniquePrices)
        {
            price.MarketPriceId = Guid.NewGuid();
            price.CollectedAt = collectedAt;
            price.LastUpdated = collectedAt;
            await _context.MarketPrices.AddAsync(price, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredAsync(int olderThanDays = 7, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

        var expiredPrices = await _context.MarketPrices
            .Where(mp => mp.CollectedAt < cutoffDate && mp.LastUpdated == null ||
                         mp.LastUpdated < cutoffDate)
            .ToListAsync(cancellationToken);

        _context.MarketPrices.RemoveRange(expiredPrices);
        await _context.SaveChangesAsync(cancellationToken);

        return expiredPrices.Count;
    }

    public async Task<MarketPriceStats?> GetPriceStatsAsync(string barcode, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var prices = await _context.MarketPrices
            .Where(mp =>
                mp.Barcode == barcode &&
                mp.Status == MarketPriceState.Active &&
                mp.CollectedAt >= fromUtc &&
                mp.CollectedAt <= toUtc)
            .ToListAsync(cancellationToken);

        if (!prices.Any())
            return null;

        return new MarketPriceStats
        {
            Barcode = barcode,
            MinPrice = prices.Min(p => p.Price),
            MaxPrice = prices.Max(p => p.Price),
            AvgPrice = prices.Average(p => p.Price),
            SourceCount = prices.Select(p => p.Source).Distinct().Count(),
            Sources = prices.Select(p => p.Source).Distinct().ToList(),
            LastUpdated = prices.Max(p => p.LastUpdated ?? p.CollectedAt)
        };
    }

    public async Task<List<MarketPrice>> GetLatestDetailsAsync(string barcode, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        var prices = await _context.MarketPrices
            .Where(mp =>
                mp.Barcode == barcode &&
                mp.Status == MarketPriceState.Active &&
                mp.CollectedAt >= fromUtc)
            .OrderByDescending(mp => mp.CollectedAt)
            .ToListAsync(cancellationToken);

        return prices
            .GroupBy(mp => new { mp.Source, StoreName = mp.StoreName ?? string.Empty })
            .Select(g => g.First())
            .OrderBy(mp => mp.Price)
            .ToList();
    }

    public async Task<DateTime?> GetLatestCollectedAtAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _context.MarketPrices
            .Where(mp => mp.Barcode == barcode)
            .MaxAsync(mp => (DateTime?)mp.CollectedAt, cancellationToken);
    }

    public async Task<List<MarketPriceRefreshTarget>> GetPublishedProductsForRefreshAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => p.Status == ProductState.Published)
            .Where(p => !string.IsNullOrWhiteSpace(p.Barcode) || !string.IsNullOrWhiteSpace(p.Name))
            .OrderByDescending(p => p.PublishedAt ?? p.UpdatedAt)
            .Select(p => new MarketPriceRefreshTarget(
                string.IsNullOrWhiteSpace(p.Barcode) ? null : p.Barcode,
                p.Name))
            .ToListAsync(cancellationToken);
    }
}


