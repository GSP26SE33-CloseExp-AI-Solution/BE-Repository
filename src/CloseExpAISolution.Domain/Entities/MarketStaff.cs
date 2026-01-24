namespace CloseExpAISolution.Domain.Entities;

public class MarketStaff
{
    public Guid MarketStaffId { get; set; }
    public Guid UserId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Position { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public Supermarket? Supermarket { get; set; }
}

