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
    public Guid? CollectionId { get; set; }
    public string? CollectionPointName { get; set; }
    public string DeliveryType { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public Guid? AddressId { get; set; }
    public Guid? PromotionId { get; set; }
    public Guid? DeliveryGroupId { get; set; }
    public string? DeliveryNote { get; set; }
    public DateTime? CancelDeadline { get; set; }
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
    public decimal TotalPrice { get; set; }
    public decimal LineTotal => TotalPrice != 0 ? TotalPrice : Quantity * UnitPrice;

    /// <summary>
    /// Product/lot info when loaded with details
    /// </summary>
    public string? ProductName { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
