namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// User account states
/// </summary>
public enum UserState
{
    /// <summary>
    /// Tài khoản vừa đăng ký qua form - chưa xác minh email
    /// Chờ user nhập OTP 6 số để xác minh email tồn tại
    /// </summary>
    Unverified,

    /// <summary>
    /// Email đã được xác minh (qua OTP hoặc Google OAuth)
    /// Đang chờ Admin phê duyệt để được sử dụng hệ thống
    /// </summary>
    PendingApproval,

    /// <summary>
    /// Tài khoản đã được Admin phê duyệt - có thể đăng nhập và hoạt động
    /// </summary>
    Active,

    /// <summary>
    /// Admin từ chối phê duyệt tài khoản
    /// </summary>
    Rejected,

    /// <summary>
    /// Tài khoản bị khóa tạm thời do đăng nhập sai quá nhiều lần (30 phút)
    /// </summary>
    Locked,

    /// <summary>
    /// Tài khoản bị Admin cấm vĩnh viễn
    /// </summary>
    Banned,

    /// <summary>
    /// Tài khoản đã bị xóa
    /// </summary>
    Deleted,

    /// <summary>
    /// Tài khoản bị ẩn khỏi public view
    /// </summary>
    Hidden
}
