namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// Notification delivery states
/// </summary>
public enum NotificationState
{
    /// <summary>
    /// Notification queued for sending
    /// </summary>
    Pending,

    /// <summary>
    /// Notification successfully sent
    /// </summary>
    Sent,

    /// <summary>
    /// Notification delivery failed
    /// </summary>
    Failed
}
