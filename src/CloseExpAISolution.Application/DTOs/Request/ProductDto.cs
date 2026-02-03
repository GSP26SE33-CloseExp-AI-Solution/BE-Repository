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
    public string Status { get; set; } = string.Empty;

    [Required(ErrorMessage = "CreatedBy is required")]
    public required string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public User? CreatedByUser { get; set; }
    public Supermarket? Supermarket { get; set; }
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    public ICollection<ProductLot> ProductLots { get; set; } = new List<ProductLot>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<AIVerificationLog> AIVerificationLogs { get; set; } = new List<AIVerificationLog>();
}

public class CreateProductRequestDto
{
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
}

public class UpdateProductRequestDto
{
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public ProductState Status { get; set; }
}

/// <summary>
/// Request to verify a draft product and set original price
/// </summary>
public class VerifyProductRequestDto
{
    /// <summary>
    /// Staff can update product info if OCR was incorrect
    /// </summary>
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Barcode { get; set; }
    
    /// <summary>
    /// Original price of the product (required for pricing calculation)
    /// </summary>
    [Required(ErrorMessage = "OriginalPrice is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "OriginalPrice must be greater than 0")]
    public decimal OriginalPrice { get; set; }
    
    /// <summary>
    /// Expiry date - can be corrected if OCR was incorrect
    /// </summary>
    public DateTime? ExpiryDate { get; set; }
    
    /// <summary>
    /// Manufacture date - optional
    /// </summary>
    public DateTime? ManufactureDate { get; set; }
    
    /// <summary>
    /// Staff ID who verified
    /// </summary>
    [Required(ErrorMessage = "VerifiedBy is required")]
    public string VerifiedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to confirm the final price and publish product
/// </summary>
public class ConfirmPriceRequestDto
{
    /// <summary>
    /// Final price after staff review. 
    /// If not provided, use the AI suggested price.
    /// </summary>
    public decimal? FinalPrice { get; set; }
    
    /// <summary>
    /// Staff feedback on the suggested price (for AI improvement)
    /// </summary>
    public string? PriceFeedback { get; set; }
    
    /// <summary>
    /// Whether the suggested price was accepted without changes
    /// </summary>
    public bool AcceptedSuggestion { get; set; }
    
    /// <summary>
    /// Staff ID who confirmed the price
    /// </summary>
    [Required(ErrorMessage = "ConfirmedBy is required")]
    public string ConfirmedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to publish a priced product
/// </summary>
public class PublishProductRequestDto
{
    /// <summary>
    /// Staff ID who published
    /// </summary>
    [Required(ErrorMessage = "PublishedBy is required")]
    public string PublishedBy { get; set; } = string.Empty;
}

public class ProductResponseDto
{
    public Guid ProductId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public ProductState Status { get; set; }
    
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
    
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}

/// <summary>
/// Response after AI pricing suggestion
/// </summary>
public class PricingSuggestionResponseDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public float Confidence { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? DaysToExpiry { get; set; }
    public List<string> Reasons { get; set; } = new();
    
    // Market comparison
    public decimal? MinMarketPrice { get; set; }
    public decimal? AvgMarketPrice { get; set; }
    public decimal? MaxMarketPrice { get; set; }
    public List<MarketPriceSourceDto> MarketPriceSources { get; set; } = new();
}

public class MarketPriceSourceDto
{
    public string StoreName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Source { get; set; } = string.Empty;
}
