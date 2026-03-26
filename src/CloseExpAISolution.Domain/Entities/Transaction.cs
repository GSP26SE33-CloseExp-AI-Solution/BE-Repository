using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class Transaction
{
    public Guid TransactionId { get; set; }

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public PaymentState PaymentStatus { get; set; } = PaymentState.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public long? PayOSOrderCode { get; set; }

    public string? PayOSPaymentLinkId { get; set; }

    public string? CheckoutUrl { get; set; }

    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}
