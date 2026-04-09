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

/// <summary>
/// Di chuyển các order item giữa các group có thể regroupable.
/// Chỉ có thể di chuyển các item trong các group Draft/Confirmed/Pending; Assigned+ là blocked.
/// </summary>
public class MoveOrderItemsToDraftGroupRequestDto
{
    /// <summary>Các order item cần di chuyển (đơn lẻ hoặc batch).</summary>
    public List<Guid> OrderItemIds { get; set; } = new();

    /// <summary>
    /// ID của group cần di chuyển, hoặc null để xóa assignment group từ các item được chọn.
    /// </summary>
    public Guid? DeliveryGroupId { get; set; }
}