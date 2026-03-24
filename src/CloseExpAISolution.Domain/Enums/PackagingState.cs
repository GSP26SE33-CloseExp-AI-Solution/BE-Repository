namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// Order packaging states
/// </summary>
public enum PackagingState
{
    Pending,
    Packaging,
    Collecting = Packaging,
    Confirmed = Pending,
    Completed,
    Packaged = Completed,
    Failed
}
