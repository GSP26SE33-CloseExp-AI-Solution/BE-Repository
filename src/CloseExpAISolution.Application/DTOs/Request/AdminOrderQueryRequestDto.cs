namespace CloseExpAISolution.Application.DTOs.Request;

public class AdminOrderQueryRequestDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }

    public string? Status { get; set; }
    public string? DeliveryType { get; set; }

    public Guid? UserId { get; set; }
    public Guid? TimeSlotId { get; set; }
    public Guid? CollectionId { get; set; }
    public Guid? DeliveryGroupId { get; set; }

    /// <summary>When true, only orders with no delivery group (pool for draft generation).</summary>
    public bool UnassignedOnly { get; set; }

    public string? Search { get; set; } // OrderCode contains

    public string SortBy { get; set; } = AdminOrderSortBy.OrderDate;
    public string SortDir { get; set; } = AdminOrderSortDir.Desc;
}

public static class AdminOrderSortBy
{
    public const string OrderDate = "orderDate";
    public const string CreatedAt = "createdAt";
    public const string UpdatedAt = "updatedAt";
    public const string FinalAmount = "finalAmount";
    public const string Status = "status";
    public const string OrderCode = "orderCode";
}

public static class AdminOrderSortDir
{
    public const string Asc = "asc";
    public const string Desc = "desc";
}

