using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Request;

public class CreateOrderRequestDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid TimeSlotId { get; set; }

    public Guid? CollectionId { get; set; }

    [Required]
    [MaxLength(50)]
    public string DeliveryType { get; set; } = string.Empty;

    [Required]
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    public Guid? AddressId { get; set; }

    public Guid? PromotionId { get; set; }
    public Guid? DeliveryGroupId { get; set; }
    public string? DeliveryNote { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal FinalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DeliveryFee { get; set; }

    public DateTime? CancelDeadline { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Order must have at least one item")]
    public List<CreateOrderItemDto> OrderItems { get; set; } = new();
}

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

public class CreateOwnOrderRequestDto
{
    [Required]
    public Guid TimeSlotId { get; set; }

    public Guid? CollectionId { get; set; }

    [Required]
    [MaxLength(50)]
    public string DeliveryType { get; set; } = string.Empty;

    public Guid? AddressId { get; set; }
    public Guid? PromotionId { get; set; }
    public string? DeliveryNote { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DeliveryFee { get; set; }

    public DateTime? CancelDeadline { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Order must have at least one item")]
    public List<CreateOrderItemDto> OrderItems { get; set; } = new();
}

public class UpdateOrderStatusRequestDto
{
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderState Status { get; set; }
}

public class UpdateOrderRequestDto
{
    public Guid? TimeSlotId { get; set; }
    public Guid? CollectionId { get; set; }

    [MaxLength(50)]
    public string? DeliveryType { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? TotalAmount { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    public Guid? AddressId { get; set; }
    public Guid? PromotionId { get; set; }
    public Guid? DeliveryGroupId { get; set; }
    public string? DeliveryNote { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? FinalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DeliveryFee { get; set; }

    public DateTime? CancelDeadline { get; set; }

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

public class UpdateOrderItemRequestDto
{
    public Guid? LotId { get; set; }

    [Range(1, int.MaxValue)]
    public int? Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitPrice { get; set; }
}
