namespace CloseExpAISolution.Application.AIService.Interfaces;

/// <summary>
/// Service for looking up product information from barcode.
/// Implements Cache & Crowd-source mechanism:
/// 1. Check local DB first
/// 2. If not found, call external APIs
/// 3. Save results to DB for future lookups
/// 4. Support manual entry and AI OCR contributions
/// </summary>
public interface IBarcodeLookupService
{
    /// <summary>
    /// Lookup product information from barcode.
    /// First checks local DB, then external APIs if not found.
    /// Results from APIs are automatically saved to DB.
    /// </summary>
    /// <param name="barcode">Product barcode (EAN-13, UPC-A, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product information if found</returns>
    Task<BarcodeProductInfo?> LookupAsync(string barcode, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lookup multiple barcodes in batch
    /// </summary>
    Task<Dictionary<string, BarcodeProductInfo?>> LookupBatchAsync(
        IEnumerable<string> barcodes, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if barcode is Vietnamese (GS1 prefix 893)
    /// </summary>
    bool IsVietnameseBarcode(string barcode);

    /// <summary>
    /// Manually add or update product information (crowd-source).
    /// Used when external APIs don't have the product.
    /// </summary>
    /// <param name="productInfo">Product information to save</param>
    /// <param name="userId">User who contributed the data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved product information</returns>
    Task<BarcodeProductInfo> SaveProductAsync(
        BarcodeProductInfo productInfo, 
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing product information
    /// </summary>
    Task<BarcodeProductInfo?> UpdateProductAsync(
        string barcode,
        BarcodeProductInfo productInfo,
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a product as verified (reviewed by staff)
    /// </summary>
    Task<bool> VerifyProductAsync(string barcode, string verifiedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search products by name or brand
    /// </summary>
    Task<IEnumerable<BarcodeProductInfo>> SearchAsync(string searchTerm, int limit = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get products pending review (manual or AI-OCR entries)
    /// </summary>
    Task<IEnumerable<BarcodeProductInfo>> GetPendingReviewAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Product information retrieved from barcode lookup
/// </summary>
public class BarcodeProductInfo
{
    public string Barcode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Manufacturer { get; set; }
    public string? Weight { get; set; }
    public string? Ingredients { get; set; }
    public Dictionary<string, string>? NutritionFacts { get; set; }
    public string? Country { get; set; }
    
    /// <summary>
    /// Data source: "database", "openfoodfacts", "openfoodfacts-vn", "upcitemdb", "manual", "ai-ocr"
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// Confidence score (0.0 - 1.0)
    /// </summary>
    public float Confidence { get; set; } = 1.0f;
    
    public DateTime LookupTimestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Indicates if the product is from Vietnam (barcode starts with 893)
    /// </summary>
    public bool IsVietnameseProduct { get; set; }
    
    /// <summary>
    /// GS1 country prefix (e.g., "893" for Vietnam)
    /// </summary>
    public string? Gs1Prefix { get; set; }

    /// <summary>
    /// Number of times this barcode has been scanned
    /// </summary>
    public int ScanCount { get; set; }

    /// <summary>
    /// Whether this product has been verified by staff
    /// </summary>
    public bool IsVerified { get; set; }
}
