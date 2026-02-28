namespace CloseExpAISolution.Domain.Enums;

public enum DeliveryState
{
    /// <summary>Đơn hàng đã sẵn sàng, chờ shipper nhận</summary>
    Ready_To_Ship,

    /// <summary>Shipper đã nhận đơn</summary>
    Picked_Up,

    /// <summary>Đang giao hàng</summary>
    In_Transit,

    /// <summary>Đã giao - chờ khách xác nhận</summary>
    Delivered_Wait_Confirm,

    /// <summary>Giao hàng thất bại</summary>
    Failed,

    /// <summary>Đã hoàn thành (khách xác nhận)</summary>
    Completed
}
