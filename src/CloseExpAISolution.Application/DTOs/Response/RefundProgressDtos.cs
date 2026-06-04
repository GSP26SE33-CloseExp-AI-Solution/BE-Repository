namespace CloseExpAISolution.Application.DTOs.Response;

public class RefundProgressStepDto
{
    public string Step { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime? OccurredAt { get; set; }
}

public class RefundOrderItemDto
{
    public Guid OrderItemId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string PackagingStatus { get; set; } = string.Empty;
    public string? DeliveryStatus { get; set; }
}

public class OrderItemRefundProgressDto
{
    public Guid RefundId { get; set; }
    public string RefundStatus { get; set; } = string.Empty;
    public decimal LineRefundAmount { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedBy { get; set; }
    public bool IsFullOrderRefund { get; set; }
    public IReadOnlyList<RefundProgressStepDto> Steps { get; set; } = Array.Empty<RefundProgressStepDto>();
}
