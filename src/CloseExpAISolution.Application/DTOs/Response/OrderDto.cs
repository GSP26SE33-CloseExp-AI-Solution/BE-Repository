namespace CloseExpAISolution.Application.DTOs.Response;

/// <summary>
/// Response DTO for order
/// </summary>
public class OrderResponseDto
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public Guid TimeSlotId { get; set; }
    public string? TimeSlotDisplay { get; set; }
    public Guid? PickupPointId { get; set; }
    public string? PickupPointName { get; set; }
    public string DeliveryType { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public Guid? DoorPickupId { get; set; }
    public Guid? PromotionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<OrderItemResponseDto> OrderItems { get; set; } = new();
}

/// <summary>
/// Response DTO for order item
/// </summary>
public class OrderItemResponseDto
{
    public Guid OrderItemId { get; set; }
    public Guid OrderId { get; set; }
    public Guid LotId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;

    /// <summary>
    /// Product/lot info when loaded with details
    /// </summary>
    public string? ProductName { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
