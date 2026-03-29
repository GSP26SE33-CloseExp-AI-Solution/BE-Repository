using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Infrastructure.Repositories;

public interface IMarketPriceRepository
{
    Task<List<MarketPrice>> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<MarketPrice?> GetMinPriceByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<List<MarketPrice>> SearchByProductNameAsync(string productName, CancellationToken cancellationToken = default);
    Task<MarketPrice> InsertObservationAsync(MarketPrice marketPrice, DateTime? batchTimestamp = null, CancellationToken cancellationToken = default);
    Task BulkInsertObservationsAsync(IEnumerable<MarketPrice> marketPrices, DateTime? batchTimestamp = null, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredAsync(int olderThanDays = 7, CancellationToken cancellationToken = default);
    Task<MarketPriceStats?> GetPriceStatsAsync(string barcode, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<List<MarketPrice>> GetLatestDetailsAsync(string barcode, DateTime fromUtc, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLatestCollectedAtAsync(string barcode, CancellationToken cancellationToken = default);
    Task<List<string>> GetDistinctBarcodesNeedingRefreshAsync(DateTime staleBeforeUtc, int take = 200, CancellationToken cancellationToken = default);
}



public class MarketPriceStats
{
    public string Barcode { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal AvgPrice { get; set; }
    public int SourceCount { get; set; }
    public List<string> Sources { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}
