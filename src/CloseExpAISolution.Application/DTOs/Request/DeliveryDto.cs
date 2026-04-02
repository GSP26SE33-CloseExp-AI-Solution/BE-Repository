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

    /// <summary>
    /// Mã quét từ QR (phải khớp <see cref="Order.OrderCode"/> khi gửi lên).
    /// Để null khi xác nhận thủ công không quét QR.
    /// </summary>
    [Required(ErrorMessage = "Mã quét từ QR là bắt buộc")]
    public string VerificationCode { get; set; } = string.Empty;
}

public class ReportDeliveryFailureRequestDto
{
    [Required(ErrorMessage = "Lý do thất bại là bắt buộc")]
    public string FailureReason { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public class StartDeliveryRequestDto
{
    public string? Notes { get; set; }
}

public class ConfirmOrderReceiptRequestDto
{
    public string? Notes { get; set; }
}
