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
    /// Packaging staff - Back-office packaging operations
    /// </summary>
    PackagingStaff = 2,

    /// <summary>
    /// Marketing staff - Manages promotions and campaigns
    /// </summary>
    MarketingStaff = 3,

    /// <summary>
    /// Supermarket staff - Works at supermarkets (nhân viên siêu thị)
    /// </summary>
    SupermarketStaff = 4,

    /// <summary>
    /// Delivery staff - Handles product delivery
    /// </summary>
    DeliveryStaff = 5,

    /// <summary>
    /// Vendor - Small restaurant/retail seller
    /// </summary>
    Vendor = 6
}
