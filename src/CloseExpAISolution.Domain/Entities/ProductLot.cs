namespace CloseExpAISolution.Domain.Entities;

public class ProductLot
{
    public Guid LotId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime ManufactureDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal Weight { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }

    public Product? Product { get; set; }
    public ICollection<InventoryDisposal> InventoryDisposals { get; set; } = new List<InventoryDisposal>();
    public ICollection<AIPriceHistory> AIPriceHistories { get; set; } = new List<AIPriceHistory>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
