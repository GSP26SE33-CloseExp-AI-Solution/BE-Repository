namespace CloseExpAISolution.Domain.Entities;

/// <summary>
/// Lô hàng - chứa thông tin hạn sử dụng và giá cả riêng cho từng lô
/// </summary>
public class ProductLot
{
    public Guid LotId { get; set; }
    public Guid ProductId { get; set; }
    public Guid UnitId { get; set; }

    // ===== THÔNG TIN HẠN SỬ DỤNG =====
    public DateTime ExpiryDate { get; set; }
    public DateTime ManufactureDate { get; set; }

    // ===== SỐ LƯỢNG =====
    public decimal Quantity { get; set; }
    public decimal Weight { get; set; }

    // ===== GIÁ CẢ =====
    public decimal OriginalUnitPrice { get; set; }
    public decimal SuggestedUnitPrice { get; set; }
    public decimal FinalUnitPrice { get; set; }

    // ===== AI PRICING =====
    /// <summary>
    /// Độ tin cậy của AI khi đề xuất giá (0-1)
    /// </summary>
    public float PricingConfidence { get; set; }

    /// <summary>
    /// Lý do AI đề xuất giá
    /// </summary>
    public string? PricingReasons { get; set; }

    // ===== WORKFLOW =====
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Người định giá
    /// </summary>
    public string? PricedBy { get; set; }
    public DateTime? PricedAt { get; set; }

    /// <summary>
    /// Người publish lên bán
    /// </summary>
    public string? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }

    // ===== NAVIGATION =====
    public Product? Product { get; set; }
    public Unit? Unit { get; set; }
    public ICollection<OverdueRecord> OverdueRecords { get; set; } = new List<OverdueRecord>();
    public ICollection<AIPriceHistory> AIPriceHistories { get; set; } = new List<AIPriceHistory>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
