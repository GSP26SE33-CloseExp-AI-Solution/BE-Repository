namespace CloseExpAISolution.Application.DTOs.Request;

public class CrawlRequest
{
    public string Barcode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
}

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
