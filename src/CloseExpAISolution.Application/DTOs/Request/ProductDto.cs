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

public class VerifyProductRequestDto
{
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Barcode { get; set; }

    [Required(ErrorMessage = "OriginalPrice is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "OriginalPrice must be greater than 0")]
    public decimal OriginalPrice { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public bool? IsFreshFood { get; set; }

    [Required(ErrorMessage = "VerifiedBy is required")]
    public string VerifiedBy { get; set; } = string.Empty;
}

public class GetPricingSuggestionRequestDto
{
    [Required(ErrorMessage = "OriginalPrice is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "OriginalPrice must be greater than 0")]
    public decimal OriginalPrice { get; set; }
}

public class ConfirmPriceRequestDto
{
    public decimal? FinalPrice { get; set; }
    public string? PriceFeedback { get; set; }
    public bool AcceptedSuggestion { get; set; }

    [Required(ErrorMessage = "ConfirmedBy is required")]
    public string ConfirmedBy { get; set; } = string.Empty;
}

public class PublishProductRequestDto
{
    [Required(ErrorMessage = "PublishedBy is required")]
    public string PublishedBy { get; set; } = string.Empty;
}

public class CreateProductLotFromExistingDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    public DateTime? ManufactureDate { get; set; }

    [Range(1, int.MaxValue)]
    public decimal Quantity { get; set; } = 1;

    public decimal Weight { get; set; } = 0;

    [Required]
    public string CreatedBy { get; set; } = string.Empty;
}

public class CreateNewProductRequestDto
{
    [Required]
    public Guid SupermarketId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    [Required]
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
    public string? NutritionFactsJson { get; set; }
    public string? Manufacturer { get; set; }
    public string? Origin { get; set; }
    public string? Description { get; set; }
    public string? StorageInstructions { get; set; }
    public string? UsageInstructions { get; set; }
    public string? OcrImageUrl { get; set; }
    public string? OcrExtractedData { get; set; }

    public float OcrConfidence { get; set; }

    [Required]
    public string CreatedBy { get; set; } = string.Empty;
}

#region Excel Import DTOs

public class ExcelColumnMappingDto
{
    public string SystemField { get; set; } = string.Empty;
    public string ExcelColumn { get; set; } = string.Empty;
    public string? TransformRule { get; set; }
}

public class ExcelPreviewRequestDto
{
    public string? FileBase64 { get; set; }
    public int PreviewRows { get; set; } = 5;
    public int HeaderRow { get; set; } = 0;
}

public class ExcelImportRequestDto
{
    [Required]
    public Guid SupermarketId { get; set; }
    [Required]
    public string ImportedBy { get; set; } = string.Empty;
    [Required]
    public List<ExcelColumnMappingDto> ColumnMappings { get; set; } = new();
    public int DataStartRow { get; set; } = 1;
    public bool SkipErrorRows { get; set; } = true;
}

public static class ProductFieldNames
{
    public const string Name = "Name";
    public const string Brand = "Brand";
    public const string Barcode = "Barcode";
    public const string Category = "Category";
    public const string Sku = "Sku";
    public const string Ingredients = "Ingredients";
    public const string Manufacturer = "Manufacturer";
    public const string Origin = "Origin";
    public const string Description = "Description";
    public const string StorageInstructions = "StorageInstructions";
    public const string UsageInstructions = "UsageInstructions";
    public const string Weight = "Weight";
    public const string IsFreshFood = "IsFreshFood";

    public static readonly string[] AllFields = new[]
    {
        Name, Brand, Barcode, Category, Sku, Ingredients,
        Manufacturer, Origin, Description, StorageInstructions,
        UsageInstructions, Weight, IsFreshFood
    };

    public static readonly Dictionary<string, string[]> CommonVietnameseNames = new()
    {
        { Name, new[] { "tên sản phẩm", "tên sp", "name", "product name", "tên" } },
        { Brand, new[] { "thương hiệu", "nhãn hiệu", "brand", "hãng" } },
        { Barcode, new[] { "mã vạch", "barcode", "mã sản phẩm", "mã sp", "ean", "upc" } },
        { Category, new[] { "danh mục", "loại", "nhóm hàng", "category", "phân loại" } },
        { Sku, new[] { "sku", "mã nội bộ", "mã hàng" } },
        { Ingredients, new[] { "thành phần", "nguyên liệu", "ingredients" } },
        { Manufacturer, new[] { "nhà sản xuất", "nsx", "manufacturer", "hãng sản xuất" } },
        { Origin, new[] { "xuất xứ", "origin", "nơi sản xuất", "made in" } },
        { Description, new[] { "mô tả", "description", "ghi chú" } },
        { Weight, new[] { "trọng lượng", "khối lượng", "weight", "quy cách", "đvt" } }
    };
}

#endregion
