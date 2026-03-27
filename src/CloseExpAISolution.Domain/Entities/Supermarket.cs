using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class Supermarket
{
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public SupermarketState Status { get; set; } = SupermarketState.PendingApproval;
    public DateTime CreatedAt { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<SupermarketStaff> SupermarketStaffs { get; set; } = new List<SupermarketStaff>();
}

