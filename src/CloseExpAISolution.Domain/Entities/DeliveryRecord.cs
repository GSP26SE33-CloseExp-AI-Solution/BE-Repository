namespace CloseExpAISolution.Domain.Entities;

public class DeliveryRecord
{
    public Guid DeliveryId { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime? DeliveredAt { get; set; }


    public User? User { get; set; }
}

