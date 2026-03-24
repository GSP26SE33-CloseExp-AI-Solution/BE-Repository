using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class IdentifyProductByBarcodeRequestDto
{
    [Required(ErrorMessage = "Barcode là bắt buộc")]
    public string Barcode { get; set; } = string.Empty;

    [Required(ErrorMessage = "SupermarketId là bắt buộc")]
    public Guid SupermarketId { get; set; }
}

public class CreateOrVerifyProductFromAiRequestDto
{
    [Required(ErrorMessage = "Product là bắt buộc")]
    public CreateNewProductRequestDto Product { get; set; } = null!;

    public bool AutoVerify { get; set; } = true;

    public bool IsManualMode { get; set; }

    public string? VerifiedBy { get; set; }
}

public class CreateStockLotWithPricingRequestDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    public DateTime? ManufactureDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity phải lớn hơn 0")]
    public decimal Quantity { get; set; } = 1;

    public decimal Weight { get; set; }

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "OriginalUnitPrice phải lớn hơn 0")]
    public decimal OriginalUnitPrice { get; set; }

    public bool AcceptedSuggestion { get; set; } = true;

    public decimal? FinalUnitPrice { get; set; }

    public string? PriceFeedback { get; set; }

    public string? ConfirmedBy { get; set; }

    public bool AutoPublish { get; set; } = true;

    public string? PublishedBy { get; set; }

    public int TimeoutSeconds { get; set; } = 20;

    public bool IsManualMode { get; set; }
}
