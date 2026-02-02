namespace CloseExpAISolution.Domain.Entities;

public class ProductLot
{
    public Guid LotId { get; set; }
    public Guid ProductId { get; set; }
    public Guid UnitId { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime ManufactureDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalWeight { get; set; }
    public decimal RemainingWeight { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public decimal SuggestedUnitPrice { get; set; }
    public decimal FinalUnitPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Product? Product { get; set; }
    public Unit? Unit { get; set; }
    public ICollection<OverdueRecord> OverdueRecords { get; set; } = new List<OverdueRecord>();
    public ICollection<AIPriceHistory> AIPriceHistories { get; set; } = new List<AIPriceHistory>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
