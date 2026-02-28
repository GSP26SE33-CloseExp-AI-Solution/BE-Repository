namespace CloseExpAISolution.Domain.Entities;

public class Product
{
    public Guid UnitId { get; set; }
    public Guid ProductId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public string Sku { get; set; } = string.Empty;

    public int QuantityType { get; set; } = 1;
    public decimal? DefaultPricePerKg { get; set; }

    public string? OcrExtractedData { get; set; }

    public string? Ingredients { get; set; }
    public string? NutritionFactsJson { get; set; }
    public string? Origin { get; set; }
    public string? MadeInCountry { get; set; }
    public string? Description { get; set; }
    public string? UsageInstructions { get; set; }
    public string? StorageInstructions { get; set; }
    public string? SafetyWarning { get; set; }
    public string? Manufacturer { get; set; }
    public string? Distributor { get; set; }

    public float OcrConfidence { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool isActive { get; set; } = true;
    public bool isFeatured { get; set; } = false;

    public Unit? Unit { get; set; }
    public User? CreatedByUser { get; set; }
    public Supermarket? Supermarket { get; set; }
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    public ICollection<ProductLot> ProductLots { get; set; } = new List<ProductLot>();
    public ICollection<AIVerificationLog> AIVerificationLogs { get; set; } = new List<AIVerificationLog>();
    public Pricing? Pricing { get; set; }
}
