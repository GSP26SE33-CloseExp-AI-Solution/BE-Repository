namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// Draft → (confirm) → Confirmed → (admin assign) → Pending → (shipper accept) → Assigned → InTransit → Completed.
/// </summary>
public enum DeliveryGroupState
{
    /// <summary>Admin gom đơn / chỉnh nhóm; có thể MoveOrder giữa các nhóm Draft.</summary>
    Draft,

    /// <summary>Admin đã gán shipper; chờ shipper gọi Accept.</summary>
    Pending,

    /// <summary>Shipper đã Accept; sẵn sàng Start → InTransit.</summary>
    Assigned,

    InTransit,
    Completed,
    Failed,

    /// <summary>Đã confirm từ Draft; chưa gán shipper — admin dùng PUT assignment.</summary>
    Confirmed = 6
}
