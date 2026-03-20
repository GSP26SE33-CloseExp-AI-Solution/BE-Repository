namespace CloseExpAISolution.Domain.Entities;

public class Transaction
{
    public Guid TransactionId { get; set; }

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Numeric order code sent to PayOS (must be unique per payment attempt).</summary>
    public long? PayOSOrderCode { get; set; }

    /// <summary>PayOS payment link id returned from create-link API.</summary>
    public string? PayOSPaymentLinkId { get; set; }

    /// <summary>Hosted checkout URL (optional snapshot for support / debugging).</summary>
    public string? CheckoutUrl { get; set; }
}
