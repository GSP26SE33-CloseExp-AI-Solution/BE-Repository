namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// Order processing states
/// </summary>
public enum OrderState
{
    /// <summary>
    /// Order created, waiting for payment
    /// </summary>
    Pending,

    /// <summary>
    /// Payment received, processing order
    /// </summary>
    Paid_Processing,

    /// <summary>
    /// Order packed and ready to ship
    /// </summary>
    Ready_To_Ship,

    /// <summary>
    /// Delivered, waiting for vendor confirmation
    /// </summary>
    Delivered_Wait_Confirm,

    /// <summary>
    /// Order completed and confirmed by vendor
    /// </summary>
    Completed,

    // Terminal states (before Pending only)

    /// <summary>
    /// Order canceled before payment
    /// </summary>
    Canceled,

    /// <summary>
    /// Payment refunded
    /// </summary>
    Refunded,

    /// <summary>
    /// Order processing failed
    /// </summary>
    Failed
}
