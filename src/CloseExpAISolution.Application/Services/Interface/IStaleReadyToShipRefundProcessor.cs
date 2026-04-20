namespace CloseExpAISolution.Application.Services.Interface;

/// <summary>
/// Quét các đơn đã ở trạng thái ReadyToShip quá N phút (cấu hình qua
/// <see cref="CloseExpAISolution.Domain.SystemConfigKeys.OrderReadyToShipMaxWaitMinutes"/>)
/// và tự chuyển sang Refunded + tạo bản ghi Refund (Pending) + enqueue email.
/// </summary>
public interface IStaleReadyToShipRefundProcessor
{
    /// <summary>Xử lý một batch. Trả về số đơn đã được chuyển sang Refunded trong lần chạy này.</summary>
    Task<int> ProcessAsync(CancellationToken cancellationToken = default);
}
