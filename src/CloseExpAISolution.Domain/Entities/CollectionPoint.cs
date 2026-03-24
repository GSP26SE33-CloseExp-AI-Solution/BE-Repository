namespace CloseExpAISolution.Domain.Entities;

public class CollectionPoint
{
    public Guid CollectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}