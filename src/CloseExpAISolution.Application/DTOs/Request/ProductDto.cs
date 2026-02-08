using System.ComponentModel.DataAnnotations;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Request;

public class ProductDto
{
    public Guid ProductId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public ProductType Type { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Nutrition { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ResponsibleOrg { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
    public bool isActive { get; set; }
    public bool isFeatured { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User? CreatedByUser { get; set; }
    public Supermarket? Supermarket { get; set; }
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    public ICollection<ProductLot> ProductLots { get; set; } = new List<ProductLot>();
    public ICollection<AIVerificationLog> AIVerificationLogs { get; set; } = new List<AIVerificationLog>();
    public Pricing? Pricing { get; set; }
}

public class CreateProductRequestDto
{
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public ProductType Type { get; set; } = ProductType.Standard;
    public string Sku { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Nutrition { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ResponsibleOrg { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
    public bool isActive { get; set; } = true;
    public bool isFeatured { get; set; } = false;
    public string[] Tags { get; set; } = Array.Empty<string>();
}

public class UpdateProductRequestDto
{
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public ProductType Type { get; set; }
    public string Sku { get; set; } = string.Empty;
    public ProductState Status { get; set; }
    public string Ingredients { get; set; } = string.Empty;
    public string Nutrition { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ResponsibleOrg { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
    public bool isActive { get; set; }
    public bool isFeatured { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Request to verify a draft product - confirm/correct OCR extracted info
/// </summary>
public class VerifyProductRequestDto
{
    /// <summary>
    /// Staff can update product info if OCR was incorrect
    /// </summary>
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Barcode { get; set; }

    /// <summary>
    /// Original price of the product (required for pricing calculation)
    /// </summary>
    [Required(ErrorMessage = "OriginalPrice is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "OriginalPrice must be greater than 0")]
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Expiry date - can be corrected if OCR was incorrect
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Manufacture date - optional
    /// </summary>
    public DateTime? ManufactureDate { get; set; }

    /// <summary>
    /// Whether the product is fresh food
    /// </summary>
    public bool? IsFreshFood { get; set; }

    /// <summary>
    /// Staff ID who verified
    /// </summary>
    [Required(ErrorMessage = "VerifiedBy is required")]
    public string VerifiedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to get pricing suggestion for a verified product
/// </summary>
public class GetPricingSuggestionRequestDto
{
    /// <summary>
    /// Original price of the product (required for pricing calculation)
    /// </summary>
    [Required(ErrorMessage = "OriginalPrice is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "OriginalPrice must be greater than 0")]
    public decimal OriginalPrice { get; set; }
}

/// <summary>
/// Request to confirm the final price and publish product
/// </summary>
public class ConfirmPriceRequestDto
{
    /// <summary>
    /// Final price after staff review. 
    /// If not provided, use the AI suggested price.
    /// </summary>
    public decimal? FinalPrice { get; set; }

    /// <summary>
    /// Staff feedback on the suggested price (for AI improvement)
    /// </summary>
    public string? PriceFeedback { get; set; }

    /// <summary>
    /// Whether the suggested price was accepted without changes
    /// </summary>
    public bool AcceptedSuggestion { get; set; }

    /// <summary>
    /// Staff ID who confirmed the price
    /// </summary>
    [Required(ErrorMessage = "ConfirmedBy is required")]
    public string ConfirmedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to publish a priced product
/// </summary>
public class PublishProductRequestDto
{
    /// <summary>
    /// Staff ID who published
    /// </summary>
    [Required(ErrorMessage = "PublishedBy is required")]
    public string PublishedBy { get; set; } = string.Empty;
}

/// <summary>
/// DTO cho thông tin ảnh sản phẩm
/// </summary>
public class ProductImageDto
{
    public Guid ProductImageId { get; set; }
    public Guid ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class ProductResponseDto
{
    public Guid ProductId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public ProductType Type { get; set; }
    public string Sku { get; set; } = string.Empty;
    public ProductState Status { get; set; }

    // Weight Type info (for variable weight products)
    public int WeightType { get; set; }
    public string WeightTypeName { get; set; } = string.Empty;
    public decimal? DefaultPricePerKg { get; set; }

    // Pricing info
    public decimal OriginalPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal FinalPrice { get; set; }

    // Expiry info
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public int? DaysToExpiry { get; set; }

    // AI info
    public float OcrConfidence { get; set; }
    public float PricingConfidence { get; set; }
    public string? PricingReasons { get; set; }

    [Required(ErrorMessage = "CreatedBy is required")]
    public required string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? PricedBy { get; set; }
    public DateTime? PricedAt { get; set; }

    // Image info
    /// <summary>
    /// Ảnh đại diện chính (ảnh đầu tiên trong danh sách)
    /// </summary>
    public string? MainImageUrl { get; set; }

    /// <summary>
    /// Tổng số ảnh của sản phẩm
    /// </summary>
    public int TotalImages { get; set; }

    /// <summary>
    /// Danh sách tất cả ảnh sản phẩm
    /// </summary>
    public ICollection<ProductImageDto> ProductImages { get; set; } = new List<ProductImageDto>();

    // Nutrition info
    /// <summary>
    /// Thành phần nguyên liệu (VD: "Sữa tươi, đường, vitamin D3...")
    /// </summary>
    public string? Ingredients { get; set; }

    /// <summary>
    /// Thông tin dinh dưỡng (parsed từ JSON)
    /// VD: {"calories": "120 kcal", "protein": "6g", "fat": "4g"}
    /// </summary>
    public Dictionary<string, string>? NutritionFacts { get; set; }

    /// <summary>
    /// Thông tin bổ sung từ barcode lookup (nếu có)
    /// User có thể dùng thông tin này để điền vào product khi verify
    /// </summary>
    public BarcodeLookupInfoDto? BarcodeLookupInfo { get; set; }
}

/// <summary>
/// Thông tin từ barcode lookup để user tham khảo khi verify
/// </summary>
public class BarcodeLookupInfoDto
{
    /// <summary>
    /// Barcode đã tra cứu
    /// </summary>
    public string Barcode { get; set; } = string.Empty;
    
    /// <summary>
    /// Tên sản phẩm từ database/API
    /// </summary>
    public string? ProductName { get; set; }
    
    /// <summary>
    /// Thương hiệu
    /// </summary>
    public string? Brand { get; set; }
    
    /// <summary>
    /// Danh mục sản phẩm
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Mô tả sản phẩm
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// URL ảnh sản phẩm từ database
    /// </summary>
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// Nhà sản xuất
    /// </summary>
    public string? Manufacturer { get; set; }
    
    /// <summary>
    /// Trọng lượng/Khối lượng
    /// </summary>
    public string? Weight { get; set; }
    
    /// <summary>
    /// Thành phần nguyên liệu
    /// </summary>
    public string? Ingredients { get; set; }
    
    /// <summary>
    /// Thông tin dinh dưỡng
    /// </summary>
    public Dictionary<string, string>? NutritionFacts { get; set; }
    
    /// <summary>
    /// Quốc gia xuất xứ
    /// </summary>
    public string? Country { get; set; }
    
    /// <summary>
    /// Nguồn dữ liệu: "database", "openfoodfacts", "manual", "ai-ocr"
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// Độ tin cậy của dữ liệu (0-1)
    /// </summary>
    public float Confidence { get; set; }
    
    /// <summary>
    /// Có phải sản phẩm Việt Nam (barcode 893)
    /// </summary>
    public bool IsVietnameseProduct { get; set; }
    
    /// <summary>
    /// Mã GS1 prefix
    /// </summary>
    public string? Gs1Prefix { get; set; }
    
    /// <summary>
    /// Số lần barcode này được quét
    /// </summary>
    public int ScanCount { get; set; }
    
    /// <summary>
    /// Sản phẩm đã được verify chưa
    /// </summary>
    public bool IsVerified { get; set; }
}

/// <summary>
/// Response after AI pricing suggestion
/// </summary>
public class PricingSuggestionResponseDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public float Confidence { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? DaysToExpiry { get; set; }
    public List<string> Reasons { get; set; } = new();

    // Market comparison
    public decimal? MinMarketPrice { get; set; }
    public decimal? AvgMarketPrice { get; set; }
    public decimal? MaxMarketPrice { get; set; }
    public List<MarketPriceSourceDto> MarketPriceSources { get; set; } = new();
}

public class MarketPriceSourceDto
{
    public string StoreName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Source { get; set; } = string.Empty;
}
