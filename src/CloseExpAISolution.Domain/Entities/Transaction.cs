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
}
