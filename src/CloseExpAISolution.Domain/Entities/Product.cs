namespace CloseExpAISolution.Domain.Entities;

public class Product
{
    public Guid ProductId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }

    /// <summary>
    /// Loại định lượng: Fixed (cố định) hoặc Variable (theo cân - kg)
    /// </summary>
    public int WeightType { get; set; } = 1; // Default: Fixed

    /// <summary>
    /// Giá mặc định trên 1 kg (chỉ dùng cho WeightType = Variable)
    /// Dùng để tính giá thực tế dựa vào khối lượng sản phẩm không cố định
    /// </summary>
    public decimal? DefaultPricePerKg { get; set; }

    // Pricing fields
    public decimal OriginalPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal FinalPrice { get; set; }

    // Expiry information (extracted from OCR)
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public int? ShelfLifeDays { get; set; }

    // OCR extracted data (stored as JSON for reference)
    public string? OcrExtractedData { get; set; }

    // AI confidence scores
    public float OcrConfidence { get; set; }
    public float PricingConfidence { get; set; }
    public string? PricingReasons { get; set; }

    // Workflow tracking
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? PricedBy { get; set; }
    public DateTime? PricedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string Status { get; set; } = string.Empty;

    public User? CreatedByUser { get; set; }
    public Supermarket? Supermarket { get; set; }
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    public ICollection<ProductLot> ProductLots { get; set; } = new List<ProductLot>();
    public ICollection<AIVerificationLog> AIVerificationLogs { get; set; } = new List<AIVerificationLog>();
}
