namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// Product availability states
/// </summary>
public enum ProductState
{
    /// <summary>
    /// Product verified by supplier and available for sale
    /// </summary>
    Verified,

    /// <summary>
    /// Product priced and ready for listing
    /// </summary>
    Priced,

    /// <summary>
    /// Product expired - permanently unavailable
    /// </summary>
    Expired,

    /// <summary>
    /// Product deleted - permanently removed
    /// </summary>
    Deleted,

    /// <summary>
    /// Draft product - hidden from public view
    /// </summary>
    Hidden
}
