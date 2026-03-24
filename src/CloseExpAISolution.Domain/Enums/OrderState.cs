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
    PaidProcessing,

    /// <summary>
    /// Legacy alias for backward compatibility with historical data
    /// </summary>
    Paid_Processing = PaidProcessing,

    /// <summary>
    /// Order packed and ready to ship
    /// </summary>
    ReadyToShip,

    /// <summary>
    /// Legacy alias for backward compatibility with historical data
    /// </summary>
    Ready_To_Ship = ReadyToShip,

    /// <summary>
    /// Delivered, waiting for vendor confirmation
    /// </summary>
    DeliveredWaitConfirm,

    /// <summary>
    /// Legacy alias for backward compatibility with historical data
    /// </summary>
    Delivered_Wait_Confirm = DeliveredWaitConfirm,

    /// <summary>
    /// Order completed and confirmed by vendor
    /// </summary>
    Completed,

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
