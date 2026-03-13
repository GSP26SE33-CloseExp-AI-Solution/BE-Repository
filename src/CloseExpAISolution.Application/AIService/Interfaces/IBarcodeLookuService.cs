namespace CloseExpAISolution.Application.AIService.Interfaces;

public interface IBarcodeLookupService
{
    Task<BarcodeProductInfo?> LookupAsync(string barcode, CancellationToken cancellationToken = default);
    Task<Dictionary<string, BarcodeProductInfo?>> LookupBatchAsync(
        IEnumerable<string> barcodes,
        CancellationToken cancellationToken = default);


    bool IsVietnameseBarcode(string barcode);

    Task<BarcodeProductInfo> SaveProductAsync(
        BarcodeProductInfo productInfo,
        string? userId = null,
        CancellationToken cancellationToken = default);

    Task<BarcodeProductInfo?> UpdateProductAsync(
        string barcode,
        BarcodeProductInfo productInfo,
        string? userId = null,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyProductAsync(string barcode, string verifiedBy, CancellationToken cancellationToken = default);

    Task<IEnumerable<BarcodeProductInfo>> SearchAsync(string searchTerm, int limit = 20, CancellationToken cancellationToken = default);

    Task<IEnumerable<BarcodeProductInfo>> GetPendingReviewAsync(CancellationToken cancellationToken = default);
}

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
    public string Source { get; set; } = string.Empty;
    public float Confidence { get; set; } = 1.0f;
    public DateTime LookupTimestamp { get; set; } = DateTime.UtcNow;
    public bool IsVietnameseProduct { get; set; }
    public string? Gs1Prefix { get; set; }
    public int ScanCount { get; set; }
    public bool IsVerified { get; set; }
}

