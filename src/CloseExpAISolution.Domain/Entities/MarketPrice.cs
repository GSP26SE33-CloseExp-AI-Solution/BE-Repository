namespace CloseExpAISolution.Domain.Entities;

/// <summary>
/// Entity lưu trữ giá thị trường của sản phẩm từ nhiều nguồn.
/// Được thu thập qua: Background Crawler, Google API, hoặc Crowdsource.
/// </summary>
public class MarketPrice
{
    public Guid MarketPriceId { get; set; }
    
    /// <summary>
    /// Mã barcode EAN-13/EAN-8
    /// </summary>
    public string Barcode { get; set; } = string.Empty;
    
    /// <summary>
    /// Tên sản phẩm (để search khi không có barcode)
    /// </summary>
    public string? ProductName { get; set; }
    
    /// <summary>
    /// Giá bán (VNĐ)
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Giá gốc trước khuyến mãi (nếu có)
    /// </summary>
    public decimal? OriginalPrice { get; set; }
    
    /// <summary>
    /// Nguồn dữ liệu: "bachhoaxanh", "winmart", "cooponline", "google", "manual", "shopee", "tiki"
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// URL nguồn dữ liệu (link sản phẩm)
    /// </summary>
    public string? SourceUrl { get; set; }
    
    /// <summary>
    /// Tên cửa hàng/website
    /// </summary>
    public string? StoreName { get; set; }
    
    /// <summary>
    /// Đơn vị tính (cái, gói, kg, ...)
    /// </summary>
    public string? Unit { get; set; }
    
    /// <summary>
    /// Khối lượng/dung tích sản phẩm
    /// </summary>
    public string? Weight { get; set; }
    
    /// <summary>
    /// Khu vực địa lý (HCM, HN, ...)
    /// </summary>
    public string? Region { get; set; }
    
    /// <summary>
    /// Còn hàng không
    /// </summary>
    public bool IsInStock { get; set; } = true;
    
    /// <summary>
    /// Thời gian thu thập dữ liệu
    /// </summary>
    public DateTime CollectedAt { get; set; }
    
    /// <summary>
    /// Thời gian cập nhật cuối
    /// </summary>
    public DateTime? LastUpdated { get; set; }
    
    /// <summary>
    /// Độ tin cậy của giá (0.0 - 1.0)
    /// </summary>
    public float Confidence { get; set; } = 1.0f;
    
    /// <summary>
    /// Trạng thái: "active", "expired", "invalid"
    /// </summary>
    public string Status { get; set; } = "active";
    
    /// <summary>
    /// Ghi chú thêm
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Entity lưu trữ phản hồi của nhân viên về giá đề xuất (để train AI)
/// </summary>
public class PriceFeedback
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Mã barcode sản phẩm
    /// </summary>
    public string Barcode { get; set; } = string.Empty;
    
    /// <summary>
    /// Tên sản phẩm
    /// </summary>
    public string? ProductName { get; set; }
    
    /// <summary>
    /// Giá gốc ban đầu
    /// </summary>
    public decimal OriginalPrice { get; set; }
    
    /// <summary>
    /// Giá AI đề xuất
    /// </summary>
    public decimal SuggestedPrice { get; set; }
    
    /// <summary>
    /// Giá nhân viên chọn (có thể = SuggestedPrice nếu accept)
    /// </summary>
    public decimal FinalPrice { get; set; }
    
    /// <summary>
    /// Phần trăm giảm giá thực tế
    /// </summary>
    public float ActualDiscountPercent { get; set; }
    
    /// <summary>
    /// Số ngày còn lại đến HSD
    /// </summary>
    public int DaysToExpire { get; set; }
    
    /// <summary>
    /// Danh mục sản phẩm
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Nhân viên đã chấp nhận giá đề xuất không
    /// </summary>
    public bool WasAccepted { get; set; }
    
    /// <summary>
    /// Feedback từ nhân viên
    /// </summary>
    public string? StaffFeedback { get; set; }
    
    /// <summary>
    /// Lý do từ chối/sửa giá (nếu có)
    /// </summary>
    public string? RejectionReason { get; set; }
    
    /// <summary>
    /// ID nhân viên
    /// </summary>
    public string? StaffId { get; set; }
    
    /// <summary>
    /// ID siêu thị
    /// </summary>
    public Guid? SupermarketId { get; set; }
    
    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Giá thị trường tham chiếu tại thời điểm đó
    /// </summary>
    public decimal? MarketPriceRef { get; set; }
    
    /// <summary>
    /// Nguồn giá thị trường tham chiếu
    /// </summary>
    public string? MarketPriceSource { get; set; }
}
