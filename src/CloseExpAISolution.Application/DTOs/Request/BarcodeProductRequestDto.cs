namespace CloseExpAISolution.Application.DTOs.Request;

public class BatchLookupRequest
{
    public List<string> Barcodes { get; set; } = new();
}

public class AddBarcodeProductRequest
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
    public string? Source { get; set; }
    public float? Confidence { get; set; }
}

public class UpdateBarcodeProductRequest
{
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
}

