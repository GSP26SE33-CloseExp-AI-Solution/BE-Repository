using CloseExpAISolution.Domain.Enums;

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
    public decimal Confidence { get; set; } = 1.0m;
    public MarketPriceState Status { get; set; } = MarketPriceState.Active;
    public string? Notes { get; set; }
}
