namespace CloseExpAISolution.Application.AIService.Interfaces;

/// <summary>
/// Service for looking up product information from barcode
/// </summary>
public interface IBarcodeLookupService
{
    /// <summary>
    /// Lookup product information from barcode using external APIs
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
    public string Source { get; set; } = string.Empty; // "openfoodfacts", "local_db", etc.
    public float Confidence { get; set; } = 1.0f;
    public DateTime LookupTimestamp { get; set; } = DateTime.UtcNow;
}
