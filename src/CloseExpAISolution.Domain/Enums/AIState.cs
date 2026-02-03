namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// AI processing states
/// </summary>
public enum AIState
{
    /// <summary>
    /// AI processing queued
    /// </summary>
    Pending,

    /// <summary>
    /// AI processing completed successfully
    /// </summary>
    Processed,

    /// <summary>
    /// AI processing failed
    /// </summary>
    Failed
}
