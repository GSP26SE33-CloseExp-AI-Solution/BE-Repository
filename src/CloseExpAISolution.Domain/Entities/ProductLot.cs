namespace CloseExpAISolution.Domain.Entities;

public class ProductLot
{
    public Guid LotId { get; set; }
    public Guid ProductId { get; set; }

    // ===== THÔNG TIN HẠN SỬ DỤNG =====
    public DateTime ExpiryDate { get; set; }
    public DateTime ManufactureDate { get; set; }

    // ===== SỐ LƯỢNG =====
    public decimal Quantity { get; set; } // user chọn 1 trong 2 cách: bán riêng từng miếng thịt hoặc bán 1 lô thì sẽ cắt ra theo yêu cầu của hệ thống
    public decimal Weight { get; set; } // x
    // ===== WORKFLOW =====
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Người publish lên bán
    /// </summary>
    public string? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }

    // ===== NAVIGATION =====
    public Product? Product { get; set; }
    public ICollection<OverdueRecord> OverdueRecords { get; set; } = new List<OverdueRecord>();
    public ICollection<AIPriceHistory> AIPriceHistories { get; set; } = new List<AIPriceHistory>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
