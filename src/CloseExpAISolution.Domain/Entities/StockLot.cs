using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class StockLot
{
    public Guid LotId { get; set; }
    public Guid ProductId { get; set; }
    public Guid UnitId { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime ManufactureDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public decimal SuggestedUnitPrice { get; set; }
    public decimal? FinalUnitPrice { get; set; }
    public decimal Weight { get; set; }
    public ProductState Status { get; set; } = ProductState.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }

    public Product? Product { get; set; }
    public UnitOfMeasure? Unit { get; set; }
    public ICollection<InventoryDisposal> InventoryDisposals { get; set; } = new List<InventoryDisposal>();
    public ICollection<PricingHistory> PricingHistories { get; set; } = new List<PricingHistory>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
