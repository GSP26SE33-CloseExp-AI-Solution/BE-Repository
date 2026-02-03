using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Infrastructure.Repositories;

public interface IMarketPriceRepository
{
    /// <summary>
    /// Lấy giá thị trường theo barcode
    /// </summary>
    Task<List<MarketPrice>> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy giá thấp nhất thị trường theo barcode
    /// </summary>
    Task<MarketPrice?> GetMinPriceByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy giá theo tên sản phẩm (fuzzy search)
    /// </summary>
    Task<List<MarketPrice>> SearchByProductNameAsync(string productName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Thêm hoặc cập nhật giá thị trường
    /// </summary>
    Task<MarketPrice> UpsertAsync(MarketPrice marketPrice, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Thêm nhiều giá cùng lúc (từ crawler)
    /// </summary>
    Task BulkUpsertAsync(IEnumerable<MarketPrice> marketPrices, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Xóa giá cũ (hết hạn)
    /// </summary>
    Task<int> DeleteExpiredAsync(int olderThanDays = 7, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy thống kê giá theo barcode
    /// </summary>
    Task<MarketPriceStats?> GetPriceStatsAsync(string barcode, CancellationToken cancellationToken = default);
}

public interface IPriceFeedbackRepository
{
    /// <summary>
    /// Lưu phản hồi giá từ nhân viên
    /// </summary>
    Task<PriceFeedback> AddAsync(PriceFeedback feedback, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy lịch sử phản hồi theo barcode
    /// </summary>
    Task<List<PriceFeedback>> GetByBarcodeAsync(string barcode, int limit = 100, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy tỷ lệ chấp nhận giá đề xuất theo category
    /// </summary>
    Task<Dictionary<string, float>> GetAcceptanceRateByCategoryAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy dữ liệu training cho AI
    /// </summary>
    Task<List<PriceFeedback>> GetTrainingDataAsync(DateTime fromDate, int limit = 10000, CancellationToken cancellationToken = default);
}

/// <summary>
/// Thống kê giá thị trường
/// </summary>
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
