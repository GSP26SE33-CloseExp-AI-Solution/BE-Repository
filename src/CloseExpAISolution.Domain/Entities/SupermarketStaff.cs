namespace CloseExpAISolution.Domain.Entities;

public class SupermarketStaff
{
    public Guid SupermarketStaffId { get; set; }
    public Guid UserId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Position { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public Supermarket? Supermarket { get; set; }
}

