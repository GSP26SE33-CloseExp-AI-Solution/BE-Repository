namespace CloseExpAISolution.Domain.Entities;

/// <summary>
/// Detail information for a product (1-1 with Product). Aligns with ER: ProductDetail.
/// </summary>
public class ProductDetail
{
    public Guid ProductDetailId { get; set; }
    public Guid ProductId { get; set; }

    public string? Brand { get; set; }
    public string? Ingredients { get; set; }
    /// <summary>JSON or text nutrition facts.</summary>
    public string? NutritionFacts { get; set; }
    public string? Origin { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? Description { get; set; }
    public string? UsageInstructions { get; set; }
    public string? StorageInstructions { get; set; }
    public string? SafetyWarning { get; set; }
    public string? Manufacturer { get; set; }
    public string? Distributor { get; set; }

    public Product? Product { get; set; }
}
