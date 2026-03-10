namespace CloseExpAISolution.Domain.Entities;

public class Product
{
    public Guid ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid SupermarketId { get; set; }
    public Guid UnitId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? UpdateBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool isFeatured { get; set; }

    public Unit? UnitOfMeasure { get; set; }
    public ProductDetail? ProductDetail { get; set; }
    public Category? CategoryRef { get; set; }
    public Supermarket? Supermarket { get; set; }
    public ICollection<ProductLot> ProductLots { get; set; } = new List<ProductLot>();
}
