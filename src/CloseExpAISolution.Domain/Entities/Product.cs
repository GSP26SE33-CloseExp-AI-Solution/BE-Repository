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

    // thêm entity Unit

    /// <summary>
    /// Loại định lượng: Fixed (cố định) hoặc Variable (theo cân - kg)
    /// </summary>
    public int QuantityType { get; set; } = 1; // Default: Fixed 

    /// bỏ từ {
    /// <summary>
    /// Giá mặc định trên 1 kg (chỉ dùng cho WeightType = Variable)
    /// Dùng để tính giá thực tế dựa vào khối lượng sản phẩm không cố định
    /// </summary>
    public decimal? DefaultPricePerKg { get; set; }

    // Pricing fields
    public decimal OriginalPrice { get; set; } // x
    public decimal SuggestedPrice { get; set; } // x
    public decimal FinalPrice { get; set; } // x

    // Expiry information (extracted from OCR)
    public DateTime? ExpiryDate { get; set; } // x
    public DateTime? ManufactureDate { get; set; } // x
    public int? ShelfLifeDays { get; set; } // x
    /// } bỏ đến đây

    // OCR extracted data (stored as JSON for reference)
    public string? OcrExtractedData { get; set; } // lưu các thông về Description, Ingredients, NutritionFactsJson, UsageInstructions, StorageInstructions, SafetyWarning, Manufacturer, Distributor để user xem và nhập vào các thông tin của product

    /// <summary>
    /// Thành phần nguyên liệu (VD: "Sữa tươi, đường, vitamin D3...")
    /// </summary>
    public string? Ingredients { get; set; }

    /// <summary>
    /// Thông tin dinh dưỡng lưu dạng JSON
    /// VD: {"calories": "120 kcal", "protein": "6g", "fat": "4g"}
    /// </summary>
    public string? NutritionFactsJson { get; set; } // format lại thành excel cho FE (giống bảng thành phần dinh dưỡng)

    // Thông tin chi tiết sản phẩm (Product Detail)
    /// <summary>
    /// Xuất xứ (VD: "Việt Nam", "Nhật Bản")
    /// </summary>
    public string? Origin { get; set; }

    /// <summary>
    /// Quốc gia sản xuất (từ barcode lookup - GS1)
    /// </summary>
    public string? Country { get; set; } // chỉnh thành MadeInCountry

    /// <summary>
    /// Mô tả sản phẩm
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Cách sử dụng
    /// </summary>
    public string? UsageInstructions { get; set; }

    /// <summary>
    /// Cách bảo quản
    /// </summary>
    public string? StorageInstructions { get; set; }

    /// <summary>
    /// Cảnh báo an toàn
    /// </summary>
    public string? SafetyWarning { get; set; }

    /// <summary>
    /// Đơn vị sản xuất/chịu trách nhiệm
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Đơn vị phân phối
    /// </summary>
    public string? Distributor { get; set; }

    /// <summary>
    /// Trọng lượng/Khối lượng (VD: "500g", "1L")
    /// </summary>
    public string? Weight { get; set; } // X - product lot

    // AI confidence scores
    public float OcrConfidence { get; set; }
    public float PricingConfidence { get; set; } // chuyển sang bảng đề xuất giá riêng
    public string? PricingReasons { get; set; } // chuyển sang bảng đề xuất giá riêng

    // Workflow tracking
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? PricedBy { get; set; } // chuyển sang bảng đề xuất giá riêng
    public DateTime? PricedAt { get; set; } // chuyển sang bảng đề xuất giá riêng
    public string? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string Status { get; set; } = string.Empty;

    public User? CreatedByUser { get; set; }
    public Supermarket? Supermarket { get; set; }
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>(); // tối đa 5 images
    public ICollection<ProductLot> ProductLots { get; set; } = new List<ProductLot>();
    public ICollection<AIVerificationLog> AIVerificationLogs { get; set; } = new List<AIVerificationLog>();
}
