using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Infrastructure.Repositories;

public interface IMarketPriceRepository
{
    Task<List<MarketPrice>> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<MarketPrice?> GetMinPriceByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<List<MarketPrice>> SearchByProductNameAsync(string productName, CancellationToken cancellationToken = default);
    Task<MarketPrice> UpsertAsync(MarketPrice marketPrice, CancellationToken cancellationToken = default);
    Task BulkUpsertAsync(IEnumerable<MarketPrice> marketPrices, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredAsync(int olderThanDays = 7, CancellationToken cancellationToken = default);
    Task<MarketPriceStats?> GetPriceStatsAsync(string barcode, CancellationToken cancellationToken = default);
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
