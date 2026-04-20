using System.Globalization;

namespace CloseExpAISolution.Application.Services.Fulfillment;

/// <summary>
/// Policy thuần (static) cho luồng auto-refund khi đơn "tắc" ở ReadyToShip:
///  - Tính deadline D = T0 + N_fixed (Phương án A trong plan).
///  - Sinh nội dung ghi chú (reason) chuẩn hóa để lưu vào OrderStatusLog.Note
///    và dùng làm Refund.Reason.
/// </summary>
public static class StaleReadyToShipPolicy
{
    /// <summary>
    /// Thời điểm đơn bị coi là quá hạn chờ sau ReadyToShip.
    /// </summary>
    public static DateTime ComputeDeadline(DateTime readyToShipAtUtc, int maxWaitMinutes)
        => readyToShipAtUtc.AddMinutes(maxWaitMinutes);

    /// <summary>
    /// Đơn có đủ điều kiện thời gian để refund (đã quá deadline) hay chưa.
    /// </summary>
    public static bool IsDueForRefund(DateTime readyToShipAtUtc, DateTime nowUtc, int maxWaitMinutes)
        => nowUtc >= ComputeDeadline(readyToShipAtUtc, maxWaitMinutes);

    /// <summary>
    /// Template lý do hệ thống khi đơn tự động chuyển sang Refunded vì quá hạn RTS.
    /// Dùng cho cả OrderStatusLog.Note và Refund.Reason để audit.
    /// </summary>
    public static string BuildRefundReason(DateTime readyToShipAtUtc, int maxWaitMinutes)
    {
        var t0 = readyToShipAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
        return $"Hệ thống tự hoàn tiền: đơn ở trạng thái Sẵn sàng giao quá {maxWaitMinutes} phút mà chưa xuất giao (mốc RTS: {t0}).";
    }
}
