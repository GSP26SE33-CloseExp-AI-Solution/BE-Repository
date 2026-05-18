namespace CloseExpAISolution.Application.DTOs.Response;

public class PackagingOrderSummaryDto
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string PackagingStatus { get; set; } = "Pending";
    public string CustomerName { get; set; } = string.Empty;
    public string TimeSlotDisplay { get; set; } = string.Empty;
    public string DeliveryType { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public decimal FinalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}

public class PackagingOrderDetailDto : PackagingOrderSummaryDto
{
    public Guid? PackagingStaffId { get; set; }
    public string? PackagingStaffName { get; set; }
    public DateTime? LastPackagedAt { get; set; }
    public IEnumerable<PackagingOrderItemDto> Items { get; set; } = new List<PackagingOrderItemDto>();
}

public class PackagingHistoryRecordDto
{
    public Guid PackagingId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? OrderItemId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public Guid UserId { get; set; }
    public string PackagingStaffName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime? PackagedAt { get; set; }
}

public class PackagingOrderItemDto
{
    public Guid OrderItemId { get; set; }
    public Guid LotId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public string? UnitName { get; set; }
    public Guid? PurchaseUnitId { get; set; }
    public string? PurchaseUnitName { get; set; }
    public string? PurchaseUnitSymbol { get; set; }
    public decimal? PurchaseQuantity { get; set; }
    public string? SupermarketName { get; set; }
    public string PackagingStatus { get; set; } = string.Empty;
    public string? DeliveryStatus { get; set; }
    public DateTime? PackagedAt { get; set; }
    public string? PackagingFailedReason { get; set; }
}
