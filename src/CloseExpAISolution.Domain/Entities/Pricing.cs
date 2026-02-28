namespace CloseExpAISolution.Domain.Entities;

public class Pricing
{
    public Guid PricingId { get; set; }
    public Guid ProductId { get; set; }
    public decimal BasePrice { get; set; }
    public string BaseUnit { get; set; } = string.Empty;
    public string Currency { get; set; } = "VND";
    public decimal? SalePrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public string? PricedBy { get; set; }
    public DateTime? PricedAt { get; set; }
    public float PricingConfidence { get; set; }
    public string? PricingReasons { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public decimal SuggestedUnitPrice { get; set; }
    public decimal FinalUnitPrice { get; set; }     
    public Product? Product { get; set; }
}
