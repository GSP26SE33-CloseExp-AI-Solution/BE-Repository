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

    public Product? Product { get; set; }
}
