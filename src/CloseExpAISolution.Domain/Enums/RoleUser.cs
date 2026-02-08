namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// User roles in the system
/// </summary>
public enum RoleUser
{
    /// <summary>
    /// System administrator - Full system access
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Internal staff - Back-office operations
    /// </summary>
    Staff = 2,

    /// <summary>
    /// Marketing staff - Manages promotions and campaigns
    /// </summary>
    MarketingStaff = 3,

    /// <summary>
    /// Supplier staff - Works at supermarkets (nhân viên siêu thị)
    /// </summary>
    SupplierStaff = 4,

    /// <summary>
    /// Delivery staff - Handles product delivery
    /// </summary>
    DeliveryStaff = 5,

    /// <summary>
    /// Vendor - Small restaurant/retail seller
    /// </summary>
    Vendor = 6
}
