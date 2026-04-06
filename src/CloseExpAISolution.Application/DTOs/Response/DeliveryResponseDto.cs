namespace CloseExpAISolution.Application.DTOs.Response;

public class DeliveryGroupResponseDto
{
    public Guid DeliveryGroupId { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public Guid? DeliveryStaffId { get; set; }
    public string? DeliveryStaffName { get; set; }
    public Guid TimeSlotId { get; set; }
    public string TimeSlotDisplay { get; set; } = string.Empty;
    public string DeliveryType { get; set; } = string.Empty;
    public string DeliveryArea { get; set; } = string.Empty;
    public decimal? CenterLatitude { get; set; }
    public decimal? CenterLongitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int FailedOrders { get; set; }
    public string? Notes { get; set; }
    public DateTime DeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public IEnumerable<DeliveryOrderResponseDto> Orders { get; set; } = new List<DeliveryOrderResponseDto>();
}

public class DeliveryOrderResponseDto
{
    public Guid OrderId { get; set; }

    /// <summary>Nhóm giao chứa đơn (để app gọi start-delivery trước khi xác nhận).</summary>
    public Guid? DeliveryGroupId { get; set; }

    public string OrderCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DeliveryType { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public DateTime OrderDate { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    public string? CollectionPointName { get; set; }
    public string? AddressLine { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? DeliveryNote { get; set; }

    public string TimeSlotDisplay { get; set; } = string.Empty;

    public int TotalItems { get; set; }
    public IEnumerable<DeliveryOrderItemDto> Items { get; set; } = new List<DeliveryOrderItemDto>();
}

public class DeliveryOrderItemDto
{
    public Guid OrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public string PackagingStatus { get; set; } = string.Empty;
    public string? DeliveryStatus { get; set; }
    public Guid? DeliveryGroupId { get; set; }
}

public class DeliveryRecordResponseDto
{
    public Guid DeliveryId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string DeliveryStaffName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public decimal? DeliveryLatitude { get; set; }
    public decimal? DeliveryLongitude { get; set; }

    /// <summary>URL ảnh chứng minh giao hàng (nếu có).</summary>
    public string? ProofImageUrl { get; set; }
}

/// <summary>Kết quả upload ảnh chứng minh trước khi gọi confirm-delivery.</summary>
public class DeliveryProofUploadResponseDto
{
    public string ProofImageUrl { get; set; } = string.Empty;
}

public class DeliveryStatsResponseDto
{
    public Guid DeliveryStaffId { get; set; }
    public string DeliveryStaffName { get; set; } = string.Empty;
    public int TotalAssignedGroups { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int FailedOrders { get; set; }
    public int PendingOrders { get; set; }
    public int InTransitOrders { get; set; }
    public decimal CompletionRate { get; set; }
    public DateTime? LastDeliveryAt { get; set; }
}

public class DeliveryGroupSummaryDto
{
    public Guid DeliveryGroupId { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string TimeSlotDisplay { get; set; } = string.Empty;
    public string DeliveryType { get; set; } = string.Empty;
    public string DeliveryArea { get; set; } = string.Empty;
    public decimal? CenterLatitude { get; set; }
    public decimal? CenterLongitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public DateTime DeliveryDate { get; set; }
}

public class MoveOrderToDraftGroupResultDto
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid? DeliveryGroupId { get; set; }
}
