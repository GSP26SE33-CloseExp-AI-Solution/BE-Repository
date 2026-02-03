using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.AIService.Models;

namespace CloseExpAISolution.Application.Services.Interface;

/// <summary>
/// Service interface for AI-powered product operations
/// </summary>
public interface IAIProductService
{
    /// <summary>
    /// Extract product information from image and create verification log
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="imageUrl">Image URL</param>
    /// <param name="lookupBarcode">Whether to lookup barcode info from external APIs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted product data with verification result</returns>
    Task<ProductExtractionResult> ExtractProductInfoAsync(
        Guid productId,
        string imageUrl,
        bool lookupBarcode = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get AI-suggested price for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pricing suggestion</returns>
    Task<PricingSuggestionResult> GetPriceSuggestionAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get AI-suggested price for product parameters
    /// </summary>
    /// <param name="category">Product category</param>
    /// <param name="expiryDate">Expiry date</param>
    /// <param name="originalPrice">Original price</param>
    /// <param name="brand">Optional brand</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pricing suggestion</returns>
    Task<PricingSuggestionResult> GetPriceSuggestionAsync(
        string category,
        DateTime expiryDate,
        decimal originalPrice,
        string? brand = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze shelf image for products
    /// </summary>
    /// <param name="imageUrl">Shelf image URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result</returns>
    Task<ShelfAnalysisResult> AnalyzeShelfImageAsync(
        string imageUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process product with full AI pipeline (OCR + Pricing)
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="imageUrl">Product image URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Full processing result</returns>
    Task<ProductProcessingResult> ProcessProductAsync(
        Guid productId,
        string imageUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if AI service is available
    /// </summary>
    Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Smart scan that automatically determines the appropriate AI endpoint based on image content.
    /// Supports both packaged products (with barcode) and fresh produce (vegetables, fruits, meat, seafood).
    /// Includes special support for Vietnamese products (barcode starting with 893).
    /// </summary>
    /// <param name="imageUrl">Image URL or base64</param>
    /// <param name="productTypeHint">Hint: "auto", "packaged", "fresh_produce", "barcode"</param>
    /// <param name="lookupBarcode">Whether to lookup barcode from external APIs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Smart scan result</returns>
    Task<SmartScanResult> SmartScanAsync(
        string imageUrl,
        string productTypeHint = "auto",
        bool lookupBarcode = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Identify fresh produce from image (vegetables, fruits, meat, seafood)
    /// Returns Vietnamese names, shelf life, storage recommendations
    /// </summary>
    /// <param name="imageUrl">Image URL or base64</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fresh produce identification result</returns>
    Task<FreshProduceResult> IdentifyFreshProduceAsync(
        string imageUrl,
        CancellationToken cancellationToken = default);
}

#region Result Models

public class ProductExtractionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Extracted data from OCR
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Barcode { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufacturedDate { get; set; }
    
    // Barcode lookup data (enriched product info)
    public BarcodeProductInfo? BarcodeInfo { get; set; }
    
    // Combined/Best product info (OCR + Barcode lookup)
    public string? BestName => BarcodeInfo?.ProductName ?? Name;
    public string? BestBrand => BarcodeInfo?.Brand ?? Brand;
    public string? Category => BarcodeInfo?.Category;
    public string? Description => BarcodeInfo?.Description;
    public string? ProductImageUrl => BarcodeInfo?.ImageUrl;
    public string? Ingredients => BarcodeInfo?.Ingredients;
    public Dictionary<string, string>? NutritionFacts => BarcodeInfo?.NutritionFacts;
    
    // Confidence scores
    public float OverallConfidence { get; set; }
    public float? ExpiryDateConfidence { get; set; }
    public float? ManufacturedDateConfidence { get; set; }
    
    // Processing info
    public float ProcessingTimeMs { get; set; }
    public List<string> Warnings { get; set; } = new();
    public bool BarcodeFound => BarcodeInfo != null;
    public string? BarcodeSource => BarcodeInfo?.Source;
    
    // Verification log ID
    public Guid? VerificationLogId { get; set; }
}

public class PricingSuggestionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Pricing data
    public decimal SuggestedPrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public float DiscountPercent { get; set; }
    public float Confidence { get; set; }
    
    /// <summary>
    /// Khả năng bán được ở mức giá này (%)
    /// </summary>
    public float ExpectedSellRate { get; set; }
    
    /// <summary>
    /// Thời gian ước tính để bán hết
    /// </summary>
    public string EstimatedTimeToSell { get; set; } = string.Empty;
    
    /// <summary>
    /// Mức cạnh tranh thị trường (0 → 1)
    /// </summary>
    public float Competitiveness { get; set; }
    
    /// <summary>
    /// Các lý do AI đưa ra mức giá này
    /// </summary>
    public List<string> Reasons { get; set; } = new();
    
    // Recommendations
    public string UrgencyLevel { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    
    // Context
    public int DaysToExpire { get; set; }
    public decimal OriginalPrice { get; set; }
    public string Category { get; set; } = string.Empty;
    
    // Processing info
    public float ProcessingTimeMs { get; set; }
}

public class ShelfAnalysisResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Detection results
    public int TotalProducts { get; set; }
    public List<DetectedProduct> Products { get; set; } = new();
    
    // Quality assessment
    public string ImageQuality { get; set; } = string.Empty;
    public float ImageQualityScore { get; set; }
    
    // Summary
    public Dictionary<string, int> CategorySummary { get; set; } = new();
    
    // Annotated image
    public string? AnnotatedImageBase64 { get; set; }
    
    // Processing info
    public float ProcessingTimeMs { get; set; }
}

public class DetectedProduct
{
    public int Index { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public BoundingBoxResult BoundingBox { get; set; } = new();
}

public class BoundingBoxResult
{
    public float X1 { get; set; }
    public float Y1 { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }
}

public class ProductProcessingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Sub-results
    public ProductExtractionResult? Extraction { get; set; }
    public PricingSuggestionResult? Pricing { get; set; }
    
    // Combined data for quick access
    public string? ExtractedName { get; set; }
    public DateTime? ExtractedExpiryDate { get; set; }
    public decimal? SuggestedPrice { get; set; }
    public float OverallConfidence { get; set; }
    
    // Processing info
    public float TotalProcessingTimeMs { get; set; }
}

/// <summary>
/// Result from smart scan operation
/// </summary>
public class SmartScanResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// The detected type of scan performed: "packaged", "fresh_produce", "mixed"
    /// </summary>
    public string ScanType { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates if this is a Vietnamese product (based on barcode starting with 893)
    /// </summary>
    public bool IsVietnameseProduct { get; set; }
    
    /// <summary>
    /// Vietnamese company info (if barcode starts with 893)
    /// </summary>
    public VietnameseBarcodeResult? VietnameseBarcodeInfo { get; set; }
    
    // Extracted data
    public string? ProductName { get; set; }
    public string? Brand { get; set; }
    public string? Barcode { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufacturedDate { get; set; }
    
    // Category and shelf life
    public string? SuggestedCategory { get; set; }
    public int? SuggestedShelfLifeDays { get; set; }
    public string? StorageRecommendation { get; set; }
    
    /// <summary>
    /// Usage instructions (Hướng dẫn sử dụng)
    /// </summary>
    public string? UsageInstructions { get; set; }
    
    // Additional info
    public List<string>? Ingredients { get; set; }
    
    /// <summary>
    /// Nutrition facts (Giá trị dinh dưỡng)
    /// </summary>
    public Dictionary<string, object>? NutritionFacts { get; set; }
    
    public string? Weight { get; set; }
    public string? Origin { get; set; }
    public List<string>? Certifications { get; set; }
    
    /// <summary>
    /// Quality standards (Chỉ tiêu chất lượng - TCVN, QCVN, etc.)
    /// </summary>
    public List<string>? QualityStandards { get; set; }
    
    /// <summary>
    /// Product warnings and notes (Cảnh báo)
    /// </summary>
    public List<string>? Warnings { get; set; }
    
    /// <summary>
    /// Manufacturer/Distributor information
    /// </summary>
    public ManufacturerInfoResult? ManufacturerInfo { get; set; }
    
    /// <summary>
    /// Product codes (SKU, Batch, MSKTVSTY)
    /// </summary>
    public ProductCodesResult? ProductCodes { get; set; }
    
    // Fresh produce detections
    public List<FreshProduceDetection>? FreshProduceItems { get; set; }
    
    // Confidence and timing
    public float Confidence { get; set; }
    public float ProcessingTimeMs { get; set; }
}

/// <summary>
/// Manufacturer and distributor information
/// </summary>
public class ManufacturerInfoResult
{
    public string? Name { get; set; }
    public string? Distributor { get; set; }
    public string? Address { get; set; }
    public List<string>? Contact { get; set; }
}

/// <summary>
/// Product identification codes
/// </summary>
public class ProductCodesResult
{
    public string? Sku { get; set; }
    public string? Batch { get; set; }
    public string? Msktvsty { get; set; }
}

/// <summary>
/// Vietnamese barcode lookup result
/// </summary>
public class VietnameseBarcodeResult
{
    public string Barcode { get; set; } = string.Empty;
    public bool IsVietnamese { get; set; }
    public string? Company { get; set; }
    public string? Category { get; set; }
    public string? Prefix { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Fresh produce detection item
/// </summary>
public class FreshProduceDetection
{
    public string Category { get; set; } = string.Empty;
    public string? NameVi { get; set; }
    public string? NameEn { get; set; }
    public int? TypicalShelfLifeDays { get; set; }
    public string? StorageRecommendation { get; set; }
    public List<string>? FreshnessIndicators { get; set; }
    public float Confidence { get; set; }
}

/// <summary>
/// Result from fresh produce identification
/// </summary>
public class FreshProduceResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<FreshProduceDetection> DetectedItems { get; set; } = new();
    public float ProcessingTimeMs { get; set; }
    public List<string>? Warnings { get; set; }
}

#endregion
