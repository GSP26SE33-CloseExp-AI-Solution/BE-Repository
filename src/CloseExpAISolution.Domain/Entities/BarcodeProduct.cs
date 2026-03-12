namespace CloseExpAISolution.Domain.Entities;

public class BarcodeProduct
{
    public Guid BarcodeProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Manufacturer { get; set; }
    public string? Weight { get; set; }
    public string? Ingredients { get; set; }
    public string? NutritionFactsJson { get; set; }
    public string? Country { get; set; }
    public string? Gs1Prefix { get; set; }
    public bool IsVietnameseProduct { get; set; }
    public string Source { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public int ScanCount { get; set; }
    public bool IsVerified { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Status { get; set; } = "active";
}
