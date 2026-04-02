namespace CloseExpAISolution.Application.DTOs.Response;

public class AdminOrderListItemDto
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string DeliveryType { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal DeliveryFee { get; set; }

    public Guid UserId { get; set; }
    public string? UserName { get; set; }

    public Guid TimeSlotId { get; set; }
    public string? TimeSlotDisplay { get; set; }

    public Guid? CollectionId { get; set; }
    public string? CollectionPointName { get; set; }

    public Guid? DeliveryGroupId { get; set; }
}

