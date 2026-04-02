using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class DeliveryLog
{
    public Guid DeliveryId { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DeliveryState? Status { get; set; } = DeliveryState.ReadyToShip;
    public string? FailedReason { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public decimal? DeliveryLatitude { get; set; }
    public decimal? DeliveryLongitude { get; set; }

    /// <summary>URL ảnh chứng minh giao hàng (http/https), thường từ R2 sau khi shipper upload.</summary>
    public string? ProofImageUrl { get; set; }

    public User? User { get; set; }
    public Order? Order { get; set; }
}

