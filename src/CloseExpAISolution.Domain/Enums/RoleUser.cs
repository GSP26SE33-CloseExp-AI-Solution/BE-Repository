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
    /// Supermarket staff - Works at physical supermarket locations
    /// </summary>
    MarketStaff = 3,

    /// <summary>
    /// Supplier staff - Works for large vendor/supplier companies
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
