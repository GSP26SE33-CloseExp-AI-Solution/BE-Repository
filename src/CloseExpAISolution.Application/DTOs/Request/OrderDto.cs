using System.ComponentModel.DataAnnotations;

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

    public Guid? DoorPickupId { get; set; }
    public Guid? PromotionId { get; set; }

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

    public Guid? DoorPickupId { get; set; }
    public Guid? PromotionId { get; set; }

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
