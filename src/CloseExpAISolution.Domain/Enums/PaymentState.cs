namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// Payment transaction states
/// </summary>
public enum PaymentState
{
    /// <summary>
    /// Payment initiated, waiting for processing
    /// </summary>
    Pending,

    /// <summary>
    /// Payment successfully completed
    /// </summary>
    Paid,

    /// <summary>
    /// Payment processing failed
    /// </summary>
    Failed
}
