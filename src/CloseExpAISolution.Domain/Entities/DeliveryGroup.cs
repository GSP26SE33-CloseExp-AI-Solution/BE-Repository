namespace CloseExpAISolution.Domain.Entities;

/// <summary>
/// Nhóm giao hàng - gom các đơn hàng theo time slot và khu vực giao hàng
/// </summary>
public class DeliveryGroup
{
    public Guid DeliveryGroupId { get; set; }

    /// <summary>Mã nhóm giao hàng (VD: DG-20260223-001)</summary>
    public string GroupCode { get; set; } = string.Empty;

    /// <summary>Shipper được giao nhóm này</summary>
    public Guid? DeliveryStaffId { get; set; }

    /// <summary>Time slot giao hàng</summary>
    public Guid TimeSlotId { get; set; }

    /// <summary>Loại giao hàng: PickupPoint / DoorToDoor</summary>
    public string DeliveryType { get; set; } = string.Empty;

    /// <summary>Khu vực giao hàng (tên pickup point hoặc khu vực)</summary>
    public string DeliveryArea { get; set; } = string.Empty;

    /// <summary>Trạng thái nhóm: Pending, Assigned, InTransit, Completed</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Tổng số đơn trong nhóm</summary>
    public int TotalOrders { get; set; }

    /// <summary>Ghi chú</summary>
    public string? Notes { get; set; }

    public DateTime DeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User? DeliveryStaff { get; set; }
    public TimeSlot? TimeSlot { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
