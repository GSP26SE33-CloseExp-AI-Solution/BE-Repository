namespace CloseExpAISolution.Application.DTOs.Response;

public class RefundResponseDto
{
    public Guid RefundId { get; set; }
    public Guid OrderId { get; set; }
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
