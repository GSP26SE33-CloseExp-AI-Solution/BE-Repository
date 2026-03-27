namespace CloseExpAISolution.Domain.Entities;

public class UnitOfMeasure
{
    public Guid UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<StockLot> StockLots { get; set; } = new List<StockLot>();
}