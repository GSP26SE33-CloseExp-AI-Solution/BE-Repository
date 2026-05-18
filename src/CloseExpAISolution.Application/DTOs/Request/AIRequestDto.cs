namespace CloseExpAISolution.Application.DTOs.Request;

public class ExtractProductRequest
{
    public Guid? ProductId { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageBase64 { get; set; }
    public bool LookupBarcode { get; set; } = true;
}

public class ImageItemRequestDto
{
    public string? ImageUrl { get; set; }
    public string? ImageBase64 { get; set; }
    /// <summary>Optional label, e.g. "front", "back", "barcode".</summary>
    public string? Label { get; set; }
}

public class ExtractMultipleRequest
{
    public List<ImageItemRequestDto> Images { get; set; } = new();
    public bool ExtractDates { get; set; } = true;
    public bool ExtractBarcode { get; set; } = true;
}

public class PricingRequest
{
    public string Category { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public decimal OriginalPrice { get; set; }
    public string? Brand { get; set; }
}

public class AnalyzeShelfRequest
{
    public string? ImageUrl { get; set; }
    public string? ImageBase64 { get; set; }
}

public class ProcessProductRequest
{
    public Guid ProductId { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageBase64 { get; set; }
}

public class SmartScanRequest
{
    public string? ImageUrl { get; set; }
    public string? ImageBase64 { get; set; }
    public string ProductTypeHint { get; set; } = "auto";
    public bool LookupBarcode { get; set; } = true;
}

public class FreshProduceRequest
{
    public string? ImageUrl { get; set; }
    public string? ImageBase64 { get; set; }
}
