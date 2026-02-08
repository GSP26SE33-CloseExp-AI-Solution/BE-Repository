namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// Trạng thái hạn sử dụng của sản phẩm/lô hàng
/// </summary>
public enum ExpiryStatus
{
    /// <summary>
    /// Trong ngày (còn dưới 24 giờ)
    /// </summary>
    Today = 1,

    /// <summary>
    /// Sắp hết hạn (1-2 ngày)
    /// </summary>
    ExpiringSoon = 2,

    /// <summary>
    /// Còn ngắn hạn (3-7 ngày)
    /// </summary>
    ShortTerm = 3,

    /// <summary>
    /// Còn dài hạn (8 ngày trở lên)
    /// </summary>
    LongTerm = 4,

    /// <summary>
    /// Đã hết hạn
    /// </summary>
    Expired = 5
}
