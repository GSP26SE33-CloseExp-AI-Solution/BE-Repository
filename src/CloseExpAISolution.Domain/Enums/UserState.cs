namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// User account states
/// </summary>
public enum UserState
{
    /// <summary>
    /// User is active and can use the system
    /// </summary>
    Active,

    /// <summary>
    /// User is temporarily inactive
    /// </summary>
    Inactive,

    /// <summary>
    /// User is permanently banned from the system
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
