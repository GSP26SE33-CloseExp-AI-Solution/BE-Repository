using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class Product
{
    public Guid ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid SupermarketId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public ProductState Status { get; set; } = ProductState.Draft;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsFeatured { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public ProductDetail? ProductDetail { get; set; }
    public Category? CategoryRef { get; set; }
    public Supermarket? Supermarket { get; set; }
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    public ICollection<StockLot> StockLots { get; set; } = new List<StockLot>();
    public ICollection<AIVerificationLog> AIVerificationLogs { get; set; } = new List<AIVerificationLog>();
}
