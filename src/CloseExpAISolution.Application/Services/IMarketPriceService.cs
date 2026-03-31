using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services;

public interface IMarketPriceService
{
    Task<MarketPriceResult?> GetMarketPriceAsync(string barcode, CancellationToken cancellationToken = default);
    Task<MarketPriceResult?> SearchMarketPriceAsync(string productName, CancellationToken cancellationToken = default);
    Task<CrawlResult> TriggerCrawlAsync(string barcode, string? productName = null, CancellationToken cancellationToken = default);
    Task<int> RefreshStaleBarcodesAsync(DateTime staleBeforeUtc, int take = 200, int concurrency = 3, CancellationToken cancellationToken = default);
    Task<MarketPrice> SaveCrowdsourcePriceAsync(CrowdsourcePriceRequest request, CancellationToken cancellationToken = default);
    Task<int> CleanupExpiredPricesAsync(int daysOld = 30, CancellationToken cancellationToken = default);
}
