namespace CloseExpAISolution.Application.DTOs.Response;

public class AdminRefundOrderSummaryDto
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string? CustomerFullName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public decimal OrderFinalAmount { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public decimal PendingRefundAmount { get; set; }
    public int RefundCount { get; set; }
    public string PrimaryRefundStatus { get; set; } = string.Empty;
    public DateTime LastRefundAt { get; set; }
}

public class AdminRefundOrderDetailDto
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string? CustomerFullName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal SystemUsageFeeAmount { get; set; }
    public decimal OrderFinalAmount { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public decimal PendingRefundAmount { get; set; }
    public IReadOnlyList<AdminRefundOrderLineItemDto> Items { get; set; } = Array.Empty<AdminRefundOrderLineItemDto>();
    public IReadOnlyList<RefundResponseDto> Refunds { get; set; } = Array.Empty<RefundResponseDto>();
}

public class AdminRefundOrderLineItemDto
{
    public Guid OrderItemId { get; set; }
    public string? ProductName { get; set; }
    public string? SupermarketName { get; set; }
    public short Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string PackagingStatus { get; set; } = string.Empty;
    public string? DeliveryStatus { get; set; }
    public bool IsRefunded { get; set; }
    public decimal? LineRefundAmount { get; set; }
    public string? RefundStatus { get; set; }
    public Guid? RefundId { get; set; }
}
