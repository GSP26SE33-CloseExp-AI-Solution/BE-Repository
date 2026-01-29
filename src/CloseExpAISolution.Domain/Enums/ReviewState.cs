namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// Review/Feedback moderation states
/// </summary>
public enum ReviewState
{
    /// <summary>
    /// Review submitted, waiting for admin approval
    /// </summary>
    Pending,

    /// <summary>
    /// Review approved by admin and visible
    /// </summary>
    Approved,

    /// <summary>
    /// Review rejected by admin
    /// </summary>
    Rejected
}
