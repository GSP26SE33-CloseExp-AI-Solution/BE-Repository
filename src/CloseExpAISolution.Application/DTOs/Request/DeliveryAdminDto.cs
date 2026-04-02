using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class AssignDeliveryGroupRequestDto
{
    [Required(ErrorMessage = "Nhân viên giao hàng là bắt buộc")]
    public Guid DeliveryStaffId { get; set; }

    public string? Reason { get; set; }
}

public class PendingDeliveryGroupQueryDto
{
    public DateTime? DeliveryDate { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public class GenerateDeliveryGroupDraftRequestDto
{
    public DateTime? DeliveryDate { get; set; }
    public Guid? TimeSlotId { get; set; }
    public Guid? CollectionId { get; set; }
    public decimal MaxDistanceKm { get; set; } = 5m;
    public int MaxOrdersPerGroup { get; set; } = 20;
}

public class DraftDeliveryGroupQueryDto
{
    public DateTime? DeliveryDate { get; set; }
    public Guid? TimeSlotId { get; set; }
    public Guid? CollectionId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>Move an order between draft groups or clear draft assignment (null).</summary>
public class MoveOrderToDraftGroupRequestDto
{
    /// <summary>Target draft group id, or null to remove the order from its current draft group.</summary>
    public Guid? DeliveryGroupId { get; set; }
}