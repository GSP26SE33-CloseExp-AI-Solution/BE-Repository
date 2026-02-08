namespace CloseExpAISolution.Domain.Entities;

/// <summary>
/// Entity lưu trữ thông tin sản phẩm theo barcode.
/// Hỗ trợ cơ chế Cache & Crowd-source:
/// - Cache từ API bên thứ 3 (Open Food Facts, UPCitemdb)
/// - Crowd-source từ nhân viên nhập liệu thủ công hoặc AI OCR
/// </summary>
public class BarcodeProduct
{
    public Guid BarcodeProductId { get; set; }
    
    /// <summary>
    /// Mã barcode EAN-13/EAN-8
    /// </summary>
    public string Barcode { get; set; } = string.Empty;
    
    /// <summary>
    /// Tên sản phẩm
    /// </summary>
    public string ProductName { get; set; } = string.Empty;
    
    /// <summary>
    /// Thương hiệu
    /// </summary>
    public string? Brand { get; set; }
    
    /// <summary>
    /// Danh mục sản phẩm
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Mô tả sản phẩm
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// URL hình ảnh sản phẩm
    /// </summary>
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// Nhà sản xuất
    /// </summary>
    public string? Manufacturer { get; set; }
    
    /// <summary>
    /// Khối lượng/dung tích (VD: "500g", "1L")
    /// </summary>
    public string? Weight { get; set; }
    
    /// <summary>
    /// Thành phần nguyên liệu
    /// </summary>
    public string? Ingredients { get; set; }
    
    /// <summary>
    /// Thông tin dinh dưỡng (JSON format)
    /// </summary>
    public string? NutritionFactsJson { get; set; }
    
    /// <summary>
    /// Quốc gia sản xuất
    /// </summary>
    public string? Country { get; set; }
    
    /// <summary>
    /// GS1 prefix (3 số đầu của barcode)
    /// </summary>
    public string? Gs1Prefix { get; set; }
    
    /// <summary>
    /// Có phải sản phẩm Việt Nam không (prefix 893)
    /// </summary>
    public bool IsVietnameseProduct { get; set; }
    
    /// <summary>
    /// Nguồn dữ liệu: "openfoodfacts", "upcitemdb", "manual", "ai-ocr"
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// Độ tin cậy của dữ liệu (0.0 - 1.0)
    /// </summary>
    public float Confidence { get; set; }
    
    /// <summary>
    /// Số lần barcode này được quét/sử dụng
    /// </summary>
    public int ScanCount { get; set; }
    
    /// <summary>
    /// Đã được xác minh bởi người dùng chưa
    /// </summary>
    public bool IsVerified { get; set; }
    
    /// <summary>
    /// Người tạo/nhập liệu (null nếu từ API)
    /// </summary>
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// Ngày tạo
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Người cập nhật cuối
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// Ngày cập nhật cuối
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Trạng thái: "active", "inactive", "pending_review"
    /// </summary>
    public string Status { get; set; } = "active";
}
