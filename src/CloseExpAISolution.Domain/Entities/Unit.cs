namespace CloseExpAISolution.Domain.Entities;

public class Unit
{
    public Guid UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Amount (e.g. quantity per unit). Aligns with ER UnitOfMeasure.amount.</summary>
    public decimal? Amount { get; set; }
    public string Type { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
