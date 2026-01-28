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

#endregion
