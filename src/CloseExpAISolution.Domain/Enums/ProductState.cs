namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// Product lifecycle states following the workflow:
/// Draft → Verified → Priced → Published → Expired/Deleted
/// </summary>
public enum ProductState
{
    /// <summary>
    /// Initial state after AI OCR extraction.
    /// Product info extracted from image, waiting for staff review.
    /// Staff needs to: verify info, enter original price, and verify product.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Staff has verified product info and entered original price.
    /// System will generate AI pricing suggestion.
    /// Staff needs to: review suggested price and confirm or adjust.
    /// </summary>
    Verified = 1,

    /// <summary>
    /// Staff has confirmed the final price.
    /// Product is ready to be published.
    /// </summary>
    Priced = 2,

    /// <summary>
    /// Product is published and visible to customers.
    /// Available for purchase.
    /// </summary>
    Published = 3,

    /// <summary>
    /// Product has expired - no longer available for sale.
    /// Auto-changed when expiry date is reached.
    /// </summary>
    Expired = 4,

    /// <summary>
    /// Product sold out - all quantity sold.
    /// </summary>
    SoldOut = 5,

    /// <summary>
    /// Product hidden from view (soft delete).
    /// Can be restored.
    /// </summary>
    Hidden = 6,

    /// <summary>
    /// Product permanently deleted.
    /// </summary>
    Deleted = 7
}
