namespace CloseExpAISolution.Domain.Entities;

public class PackagingRecord
{
    public Guid PackagingId { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PackagedAt { get; set; }

    public User? User { get; set; }
    public Order? Order { get; set; }
}

