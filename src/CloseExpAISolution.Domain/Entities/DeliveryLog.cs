namespace CloseExpAISolution.Domain.Entities;

public class DeliveryLog
{
    public Guid DeliveryId { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FailedReason { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public decimal? DeliveryLatitude { get; set; }
    public decimal? DeliveryLongitude { get; set; }

    public User? User { get; set; }
    public Order? Order { get; set; }
}

