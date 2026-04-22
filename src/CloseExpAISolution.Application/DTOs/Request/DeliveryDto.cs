using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class AcceptDeliveryGroupRequestDto
{
    public string? Notes { get; set; }
}

public class UpdateDeliveryStatusRequestDto
{
    [Required(ErrorMessage = "Trạng thái giao hàng là bắt buộc")]
    public string Status { get; set; } = string.Empty;

    public string? FailureReason { get; set; }

    public string? Notes { get; set; }
}

public class ConfirmDeliveryRequestDto
{
    [Required(ErrorMessage = "Ảnh chứng minh là bắt buộc")]
    public string ProofImageUrl { get; set; } = string.Empty;

    public string? Notes { get; set; }

    /// <summary>Mã quét từ QR; phải khớp <see cref="Order.OrderCode"/> (không phân biệt hoa thường).</summary>
    [Required(ErrorMessage = "Mã quét từ QR là bắt buộc")]
    public string VerificationCode { get; set; } = string.Empty;

    /// <summary>
    /// Nhóm giao shipper đang thao tác (app gửi từ route/context). Tránh chọn nhầm nhóm khi đơn có item ở nhiều <see cref="DeliveryGroup"/>.
    /// </summary>
    public Guid? DeliveryGroupId { get; set; }

    /// <summary>
    /// Lines being delivered in this confirmation; when null/empty, all eligible lines in the order for this shipper's group are confirmed (legacy).
    /// </summary>
    public IReadOnlyList<Guid>? OrderItemIds { get; set; }
}

public class ReportDeliveryFailureRequestDto
{
    [Required(ErrorMessage = "Lý do thất bại là bắt buộc")]
    public string FailureReason { get; set; } = string.Empty;

    public string? Notes { get; set; }

    /// <summary>
    /// Nhóm giao shipper đang thao tác; đồng bộ với <see cref="ConfirmDeliveryRequestDto.DeliveryGroupId"/>.
    /// </summary>
    public Guid? DeliveryGroupId { get; set; }

    /// <summary>
    /// Lines that failed delivery; when null/empty, whole order fails (legacy).
    /// </summary>
    public IReadOnlyList<Guid>? OrderItemIds { get; set; }
}

public class StartDeliveryRequestDto
{
    public string? Notes { get; set; }
}

public class ConfirmOrderReceiptRequestDto
{
    public string? Notes { get; set; }
}
