namespace CloseExpAISolution.Domain.Entities;

public class PickupPoint
{
    public Guid PickupPointId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

