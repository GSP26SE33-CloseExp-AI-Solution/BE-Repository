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