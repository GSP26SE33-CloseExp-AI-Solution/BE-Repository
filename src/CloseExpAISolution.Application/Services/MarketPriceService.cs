using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace CloseExpAISolution.Application.Services;

public class MarketPriceService : IMarketPriceService
{
    private readonly IMarketPriceRepository _marketPriceRepository;
    private readonly IAIServiceClient _aiClient;
    private readonly ILogger<MarketPriceService> _logger;
    private readonly IMemoryCache _cache;

    public MarketPriceService(
        IMarketPriceRepository marketPriceRepository,
        IAIServiceClient aiClient,
        ILogger<MarketPriceService> logger,
        IMemoryCache cache)
    {
        _marketPriceRepository = marketPriceRepository;
        _aiClient = aiClient;
        _logger = logger;
        _cache = cache;
    }

    public async Task<MarketPriceResult?> GetMarketPriceAsync(string barcode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting market price for barcode: {Barcode}", barcode);
        var cacheKey = $"market-feature:{barcode}";
        if (_cache.TryGetValue(cacheKey, out MarketPriceResult? cached) && cached != null)
        {
            return cached;
        }

        var now = DateTime.UtcNow;
        var stats24h = await _marketPriceRepository.GetPriceStatsAsync(barcode, now.AddHours(-24), now, cancellationToken);
        var stats7d = await _marketPriceRepository.GetPriceStatsAsync(barcode, now.AddDays(-7), now, cancellationToken);
        var selectedStats = SelectStatsByFreshness(stats24h, stats7d, now);

        if (selectedStats != null)
        {
            var detailsFrom = selectedStats == stats24h ? now.AddHours(-24) : now.AddDays(-7);
            var prices = await _marketPriceRepository.GetLatestDetailsAsync(barcode, detailsFrom, cancellationToken);

            var result = new MarketPriceResult
            {
                MinPrice = selectedStats.MinPrice,
                MaxPrice = selectedStats.MaxPrice,
                AvgPrice = selectedStats.AvgPrice,
                SourceCount = selectedStats.SourceCount,
                Sources = selectedStats.Sources,
                LastUpdated = selectedStats.LastUpdated,
                Details = prices.Select(p => new MarketPriceDetail
                {
                    Source = p.Source,
                    StoreName = p.StoreName ?? p.Source,
                    Price = p.Price,
                    OriginalPrice = p.OriginalPrice,
                    SourceUrl = p.SourceUrl,
                    IsInStock = p.IsInStock,
                    CollectedAt = p.CollectedAt
                }).ToList()
            };
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(20));
            return result;
        }

        _logger.LogInformation("No cached price for barcode {Barcode}, will need to crawl", barcode);
        return null;
    }

    public async Task<MarketPriceResult?> SearchMarketPriceAsync(string productName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching market price for product: {ProductName}", productName);

        var prices = await _marketPriceRepository.SearchByProductNameAsync(productName, cancellationToken);

        if (!prices.Any())
        {
            _logger.LogInformation("No prices found for product: {ProductName}", productName);
            return null;
        }

        return new MarketPriceResult
        {
            MinPrice = prices.Min(p => p.Price),
            MaxPrice = prices.Max(p => p.Price),
            AvgPrice = prices.Average(p => p.Price),
            SourceCount = prices.Select(p => p.Source).Distinct().Count(),
            Sources = prices.Select(p => p.Source).Distinct().ToList(),
            LastUpdated = prices.Max(p => p.LastUpdated ?? p.CollectedAt),
            Details = prices.Select(p => new MarketPriceDetail
            {
                Source = p.Source,
                StoreName = p.StoreName ?? p.Source,
                Price = p.Price,
                OriginalPrice = p.OriginalPrice,
                SourceUrl = p.SourceUrl,
                IsInStock = p.IsInStock,
                CollectedAt = p.CollectedAt
            }).ToList()
        };
    }

    public async Task<CrawlResult> TriggerCrawlAsync(string barcode, string? productName = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Triggering crawl for barcode: {Barcode}, name: {ProductName}", barcode, productName);

        try
        {
            var crawlResponse = await _aiClient.CrawlMarketPricesAsync(barcode, productName, cancellationToken);

            if (crawlResponse == null || !crawlResponse.Prices.Any())
            {
                return new CrawlResult
                {
                    Success = false,
                    PricesFound = 0,
                    Error = crawlResponse?.Error ?? "No prices found from crawlers"
                };
            }

            var marketPrices = crawlResponse.Prices.Select(p => new MarketPrice
            {
                Barcode = barcode,
                ProductName = p.ProductName ?? productName,
                Price = p.Price,
                OriginalPrice = p.OriginalPrice,
                Source = p.Source,
                SourceUrl = p.Url,
                StoreName = p.StoreName,
                Unit = p.Unit,
                Weight = p.Weight,
                Region = "VN",
                IsInStock = p.IsInStock,
                Confidence = (decimal)p.Confidence,
                Status = MarketPriceState.Active
            }).ToList();

            var crawlAt = DateTime.UtcNow;
            await _marketPriceRepository.BulkInsertObservationsAsync(marketPrices, crawlAt, cancellationToken);
            _logger.LogInformation("Market crawl inserted {Count} observations for barcode {Barcode}", marketPrices.Count, barcode);

            return new CrawlResult
            {
                Success = true,
                PricesFound = marketPrices.Count,
                Sources = marketPrices.Select(p => p.Source).Distinct().ToList(),
                MinPrice = marketPrices.Min(p => p.Price),
                AvgPrice = marketPrices.Average(p => p.Price)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crawling market prices for barcode: {Barcode}", barcode);
            return new CrawlResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<MarketPrice> SaveCrowdsourcePriceAsync(CrowdsourcePriceRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving crowdsource price for barcode: {Barcode} from staff: {StaffId}",
            request.Barcode, request.StaffId);

        var marketPrice = new MarketPrice
        {
            Barcode = request.Barcode,
            ProductName = request.ProductName,
            Price = request.Price,
            OriginalPrice = request.OriginalPrice,
            Source = "crowdsource",
            StoreName = request.StoreName,
            Region = request.Region ?? "VN",
            IsInStock = request.IsInStock,
            Confidence = 0.7m,
            Status = MarketPriceState.Active,
            Notes = $"Entered by staff {request.StaffId} at supermarket {request.SupermarketId}. {request.Note}"
        };

        return await _marketPriceRepository.InsertObservationAsync(marketPrice, DateTime.UtcNow, cancellationToken);
    }

    public async Task<int> CleanupExpiredPricesAsync(int daysOld = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning up market prices older than {Days} days", daysOld);
        return await _marketPriceRepository.DeleteExpiredAsync(daysOld, cancellationToken);
    }

    public async Task<IEnumerable<MarketPriceHistoryItemDto>> GetPriceHistoryAsync(string barcode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching price history for barcode: {Barcode}", barcode);

        var normalizedBarcode = barcode.Trim();
        var history = await _marketPriceRepository.GetByBarcodeAsync(normalizedBarcode, cancellationToken);

        return history
            .OrderByDescending(m => m.CollectedAt)
            .Select(m => new MarketPriceHistoryItemDto
            {
                MarketPriceId = m.MarketPriceId,
                Barcode = m.Barcode,
                ProductName = m.ProductName,
                Price = m.Price,
                OriginalPrice = m.OriginalPrice,
                Source = m.Source,
                StoreName = m.StoreName ?? m.Source,
                SourceUrl = m.SourceUrl,
                Unit = m.Unit,
                Weight = m.Weight,
                Region = m.Region,
                IsInStock = m.IsInStock,
                Confidence = m.Confidence,
                Status = m.Status.ToString(),
                CollectedAt = m.CollectedAt,
                LastUpdated = m.LastUpdated,
                Notes = m.Notes
            });
    }

    public async Task<int> RefreshPublishedProductsAsync(int concurrency = 3, CancellationToken cancellationToken = default)
    {
        var targets = await _marketPriceRepository.GetPublishedProductsForRefreshAsync(cancellationToken);
        if (!targets.Any())
            return 0;

        var successCount = 0;
        using var throttle = new SemaphoreSlim(Math.Clamp(concurrency, 1, 10));
        var tasks = targets.Select(async target =>
        {
            await throttle.WaitAsync(cancellationToken);
            try
            {
                if (string.IsNullOrWhiteSpace(target.Barcode) && string.IsNullOrWhiteSpace(target.ProductName))
                {
                    _logger.LogWarning("Skipping published product refresh because both barcode and product name are empty");
                    return;
                }

                var result = await TriggerCrawlAsync(target.Barcode ?? string.Empty, target.ProductName, cancellationToken);
                if (result.Success) Interlocked.Increment(ref successCount);
            }
            finally
            {
                throttle.Release();
            }
        });
        await Task.WhenAll(tasks);

        _logger.LogInformation("Market refresh job processed {Total} published products, success={Success}", targets.Count, successCount);
        return successCount;
    }

    private static MarketPriceStats? SelectStatsByFreshness(MarketPriceStats? stats24h, MarketPriceStats? stats7d, DateTime now)
    {
        if (stats24h != null && stats24h.SourceCount > 0 && stats24h.LastUpdated >= now.AddHours(-24))
        {
            return stats24h;
        }

        return stats7d;
    }
}

