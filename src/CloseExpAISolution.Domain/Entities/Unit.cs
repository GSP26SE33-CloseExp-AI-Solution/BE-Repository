namespace CloseExpAISolution.Domain.Entities;

public class Unit
{
    public Guid UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    public ICollection<ProductLot> ProductLots { get; set; } = new List<ProductLot>();
}
