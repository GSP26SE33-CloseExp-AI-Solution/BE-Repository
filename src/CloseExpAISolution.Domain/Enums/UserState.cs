namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// User account states
/// </summary>
public enum UserState
{
    /// <summary>
    /// Tài khoản chưa được xác minh - chờ Admin phê duyệt
    /// Account not verified - waiting for Admin approval
    /// </summary>
    Unverified,

    /// <summary>
    /// Tài khoản đã được xác minh bởi Admin - có thể hoạt động trên nền tảng
    /// Account verified by Admin - can operate on the platform
    /// </summary>
    Verified,

    /// <summary>
    /// Tài khoản bị khóa tạm thời do đăng nhập sai quá nhiều lần (30 phút)
    /// Account temporarily locked due to too many failed login attempts (30 minutes)
    /// </summary>
    Locked,

    /// <summary>
    /// User is permanently banned from the system by Admin
    /// </summary>
    Banned,

    /// <summary>
    /// User account permanently deleted
    /// </summary>
    Deleted,

    /// <summary>
    /// User account hidden from public view
    /// </summary>
    Hidden
}
