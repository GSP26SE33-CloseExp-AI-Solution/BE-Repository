using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Request;

/// <summary>
/// Request DTO for creating an order
/// </summary>
public class CreateOrderRequestDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid TimeSlotId { get; set; }

    public Guid? PickupPointId { get; set; }

    [Required]
    [MaxLength(50)]
    public string DeliveryType { get; set; } = string.Empty;

    [Required]
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [Required]
    public Guid AddressId { get; set; }

    public Guid? PromotionId { get; set; }
    public Guid? DeliveryGroupId { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryNote { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal FinalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DeliveryFee { get; set; }

    public DateTime? CancelDeadline { get; set; }

    /// <summary>
    /// Order items: LotId, Quantity, UnitPrice per line
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Order must have at least one item")]
    public List<CreateOrderItemDto> OrderItems { get; set; } = new();
}

/// <summary>
/// Single order line for create
/// </summary>
public class CreateOrderItemDto
{
    [Required]
    public Guid LotId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Request DTO for changing order status in one click (PUT api/orders/{id}/status).
/// Accepts <see cref="OrderState"/> values, e.g. "Pending", "Paid_Processing", "Ready_To_Ship", "Completed", "Canceled", etc.
/// </summary>
public class UpdateOrderStatusRequestDto
{
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderState Status { get; set; }
}

/// <summary>
/// Request DTO for updating an order
/// </summary>
public class UpdateOrderRequestDto
{
    public Guid? TimeSlotId { get; set; }
    public Guid? PickupPointId { get; set; }

    [MaxLength(50)]
    public string? DeliveryType { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? TotalAmount { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    public Guid? AddressId { get; set; }
    public Guid? PromotionId { get; set; }
    public Guid? DeliveryGroupId { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryNote { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? FinalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DeliveryFee { get; set; }

    public DateTime? CancelDeadline { get; set; }

    /// <summary>
    /// If provided, replaces all order items
    /// </summary>
    public List<UpdateOrderItemDto>? OrderItems { get; set; }
}

public class UpdateOrderItemDto
{
    public Guid OrderItemId { get; set; }
    public Guid LotId { get; set; }
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Request DTO for creating a standalone order item
/// </summary>
public class CreateOrderItemRequestDto
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public Guid LotId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Request DTO for updating an order item
/// </summary>
public class UpdateOrderItemRequestDto
{
    public Guid? LotId { get; set; }

    [Range(1, int.MaxValue)]
    public int? Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitPrice { get; set; }
}
