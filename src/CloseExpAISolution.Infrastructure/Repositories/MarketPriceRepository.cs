using CloseExpAISolution.Domain.Entities;
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
            .Where(mp => mp.Barcode == barcode && mp.Status == "active")
            .OrderBy(mp => mp.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task<MarketPrice?> GetMinPriceByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _context.MarketPrices
            .Where(mp => mp.Barcode == barcode && mp.Status == "active" && mp.IsInStock)
            .OrderBy(mp => mp.Price)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<MarketPrice>> SearchByProductNameAsync(string productName, CancellationToken cancellationToken = default)
    {
        var searchTerms = productName.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        return await _context.MarketPrices
            .Where(mp => mp.Status == "active" && 
                         mp.ProductName != null &&
                         searchTerms.All(term => mp.ProductName.ToLower().Contains(term)))
            .OrderBy(mp => mp.Price)
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    public async Task<MarketPrice> UpsertAsync(MarketPrice marketPrice, CancellationToken cancellationToken = default)
    {
        var existing = await _context.MarketPrices
            .FirstOrDefaultAsync(mp => 
                mp.Barcode == marketPrice.Barcode && 
                mp.Source == marketPrice.Source,
                cancellationToken);

        if (existing != null)
        {
            existing.Price = marketPrice.Price;
            existing.OriginalPrice = marketPrice.OriginalPrice;
            existing.SourceUrl = marketPrice.SourceUrl;
            existing.IsInStock = marketPrice.IsInStock;
            existing.LastUpdated = DateTime.UtcNow;
            existing.Confidence = marketPrice.Confidence;
            existing.Notes = marketPrice.Notes;
            _context.MarketPrices.Update(existing);
        }
        else
        {
            marketPrice.MarketPriceId = Guid.NewGuid();
            marketPrice.CollectedAt = DateTime.UtcNow;
            await _context.MarketPrices.AddAsync(marketPrice, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing ?? marketPrice;
    }

    public async Task BulkUpsertAsync(IEnumerable<MarketPrice> marketPrices, CancellationToken cancellationToken = default)
    {
        // Group by Barcode + Source + StoreName to handle duplicates from same crawl
        var uniquePrices = marketPrices
            .GroupBy(p => new { p.Barcode, p.Source, p.StoreName })
            .Select(g => g.First()) // Take first if duplicates
            .ToList();

        foreach (var price in uniquePrices)
        {
            var existing = await _context.MarketPrices
                .FirstOrDefaultAsync(mp => 
                    mp.Barcode == price.Barcode && 
                    mp.Source == price.Source &&
                    mp.StoreName == price.StoreName,
                    cancellationToken);

            if (existing != null)
            {
                existing.Price = price.Price;
                existing.OriginalPrice = price.OriginalPrice;
                existing.SourceUrl = price.SourceUrl;
                existing.IsInStock = price.IsInStock;
                existing.LastUpdated = DateTime.UtcNow;
                existing.ProductName = price.ProductName;
                existing.Confidence = price.Confidence;
            }
            else
            {
                price.MarketPriceId = Guid.NewGuid();
                price.CollectedAt = DateTime.UtcNow;
                await _context.MarketPrices.AddAsync(price, cancellationToken);
            }
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

    public async Task<MarketPriceStats?> GetPriceStatsAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var prices = await _context.MarketPrices
            .Where(mp => mp.Barcode == barcode && mp.Status == "active")
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
}

public class PriceFeedbackRepository : IPriceFeedbackRepository
{
    private readonly ApplicationDbContext _context;

    public PriceFeedbackRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PriceFeedback> AddAsync(PriceFeedback feedback, CancellationToken cancellationToken = default)
    {
        if (feedback.Id == Guid.Empty)
        {
            feedback.Id = Guid.NewGuid();
        }
        feedback.CreatedAt = DateTime.UtcNow;
        
        await _context.PriceFeedbacks.AddAsync(feedback, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        return feedback;
    }

    public async Task<List<PriceFeedback>> GetByBarcodeAsync(string barcode, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.PriceFeedbacks
            .Where(pf => pf.Barcode == barcode)
            .OrderByDescending(pf => pf.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, float>> GetAcceptanceRateByCategoryAsync(CancellationToken cancellationToken = default)
    {
        var stats = await _context.PriceFeedbacks
            .Where(pf => pf.Category != null)
            .GroupBy(pf => pf.Category!)
            .Select(g => new 
            {
                Category = g.Key,
                AcceptanceRate = (float)g.Count(pf => pf.WasAccepted) / g.Count()
            })
            .ToDictionaryAsync(x => x.Category, x => x.AcceptanceRate, cancellationToken);

        return stats;
    }

    public async Task<List<PriceFeedback>> GetTrainingDataAsync(DateTime fromDate, int limit = 10000, CancellationToken cancellationToken = default)
    {
        return await _context.PriceFeedbacks
            .Where(pf => pf.CreatedAt >= fromDate)
            .OrderByDescending(pf => pf.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
