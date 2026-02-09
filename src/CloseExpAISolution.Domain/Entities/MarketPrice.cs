namespace CloseExpAISolution.Domain.Entities;

public class MarketPrice
{
    public Guid MarketPriceId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public string? StoreName { get; set; }
    public string? Unit { get; set; }
    public string? Weight { get; set; }
    public string? Region { get; set; }
    public bool IsInStock { get; set; } = true;
    public DateTime CollectedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
    public float Confidence { get; set; } = 1.0f;
    public string Status { get; set; } = "active";
    public string? Notes { get; set; }
}

public class PriceFeedback
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public float ActualDiscountPercent { get; set; }
    public int DaysToExpire { get; set; }
    public string? Category { get; set; }
    public bool WasAccepted { get; set; }
    public string? StaffFeedback { get; set; }
    public string? RejectionReason { get; set; }
    public string? StaffId { get; set; }
    public Guid? SupermarketId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? MarketPriceRef { get; set; }
    public string? MarketPriceSource { get; set; }
}
