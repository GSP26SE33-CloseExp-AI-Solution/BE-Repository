using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services;

/// <summary>
/// Service interface for market price operations
/// </summary>
public interface IMarketPriceService
{
    /// <summary>
    /// Lấy thông tin giá thị trường cho sản phẩm theo barcode
    /// </summary>
    Task<MarketPriceResult?> GetMarketPriceAsync(string barcode, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy thông tin giá thị trường theo tên sản phẩm
    /// </summary>
    Task<MarketPriceResult?> SearchMarketPriceAsync(string productName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Trigger crawl giá thị trường từ AI service
    /// </summary>
    Task<CrawlResult> TriggerCrawlAsync(string barcode, string? productName = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lưu thông tin giá từ nhân viên (crowdsource)
    /// </summary>
    Task<MarketPrice> SaveCrowdsourcePriceAsync(CrowdsourcePriceRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lưu feedback từ nhân viên về giá AI đề xuất
    /// </summary>
    Task<PriceFeedback> SavePriceFeedbackAsync(PriceFeedbackRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy thống kê độ chính xác của AI theo category
    /// </summary>
    Task<Dictionary<string, float>> GetAIAccuracyByCategoryAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Xóa các giá đã cũ (quá hạn)
    /// </summary>
    Task<int> CleanupExpiredPricesAsync(int daysOld = 30, CancellationToken cancellationToken = default);
}

/// <summary>
/// Kết quả lấy giá thị trường
/// </summary>
public class MarketPriceResult
{
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? AvgPrice { get; set; }
    public int SourceCount { get; set; }
    public List<string> Sources { get; set; } = new();
    public List<MarketPriceDetail> Details { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Chi tiết giá từ từng nguồn
/// </summary>
public class MarketPriceDetail
{
    public string Source { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? SourceUrl { get; set; }
    public bool IsInStock { get; set; }
    public DateTime CollectedAt { get; set; }
}

/// <summary>
/// Kết quả crawl giá
/// </summary>
public class CrawlResult
{
    public bool Success { get; set; }
    public int PricesFound { get; set; }
    public List<string> Sources { get; set; } = new();
    public string? Error { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? AvgPrice { get; set; }
}

/// <summary>
/// Request lưu giá từ nhân viên (crowdsource)
/// </summary>
public class CrowdsourcePriceRequest
{
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? Region { get; set; }
    public Guid? StaffId { get; set; }
    public Guid? SupermarketId { get; set; }
    public bool IsInStock { get; set; } = true;
    public string? Note { get; set; }
}

/// <summary>
/// Request lưu feedback về giá AI
/// </summary>
public class PriceFeedbackRequest
{
    public string Barcode { get; set; } = string.Empty;
    public decimal SuggestedPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public int DaysToExpire { get; set; }
    public string? Category { get; set; }
    public bool WasAccepted { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? StaffId { get; set; }
    public Guid? SupermarketId { get; set; }
    public decimal? MarketPriceRef { get; set; }
    public string? MarketPriceSource { get; set; }
}
