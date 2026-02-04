using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Response;

/// <summary>
/// DTO chi tiết đầy đủ của sản phẩm - hiển thị như thông tin sản phẩm trong siêu thị
/// </summary>
public class ProductDetailDto
{
    // ===== THÔNG TIN CƠ BẢN =====
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // ===== THÔNG TIN SẢN PHẨM (như hình ảnh) =====
    /// <summary>
    /// Thương hiệu
    /// </summary>
    public string Brand { get; set; } = string.Empty;

    /// <summary>
    /// Xuất xứ (VD: "Việt Nam", "Nhật Bản")
    /// </summary>
    public string? Origin { get; set; }

    /// <summary>
    /// Trọng lượng/Khối lượng (VD: "500g", "1L", "Đang cập nhật")
    /// </summary>
    public string Weight { get; set; } = "Đang cập nhật";

    /// <summary>
    /// Thành phần nguyên liệu
    /// </summary>
    public string Ingredients { get; set; } = "Chưa có mô tả chi tiết";

    /// <summary>
    /// Cách sử dụng
    /// </summary>
    public string UsageInstructions { get; set; } = "Chưa có mô tả chi tiết";

    /// <summary>
    /// Cách bảo quản
    /// </summary>
    public string StorageInstructions { get; set; } = "Chưa có mô tả chi tiết";

    /// <summary>
    /// Ngày sản xuất (NSX)
    /// </summary>
    public string ManufactureDate { get; set; } = "Xem trên bao bì";

    /// <summary>
    /// Hạn sử dụng (HSD)
    /// </summary>
    public string ExpiryDate { get; set; } = "Xem trên bao bì";

    /// <summary>
    /// Đơn vị sản xuất/chịu trách nhiệm
    /// </summary>
    public string Manufacturer { get; set; } = "Chưa có mô tả chi tiết";

    /// <summary>
    /// Cảnh báo an toàn
    /// </summary>
    public string SafetyWarning { get; set; } = "Chưa có mô tả chi tiết";

    /// <summary>
    /// Đơn vị phân phối/Tổ chức chịu trách nhiệm
    /// </summary>
    public string Distributor { get; set; } = "Chưa có mô tả chi tiết";

    // ===== THÔNG TIN DINH DƯỠNG =====
    /// <summary>
    /// Thông tin dinh dưỡng (parsed từ JSON)
    /// VD: {"calories": "120 kcal", "protein": "6g", "fat": "4g"}
    /// </summary>
    public Dictionary<string, string>? NutritionFacts { get; set; }

    // ===== THÔNG TIN BÁN HÀNG =====
    /// <summary>
    /// Danh mục/Loại sản phẩm
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Đơn vị tính (VD: "Hộp", "Gói", "Kg")
    /// </summary>
    public string UnitName { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng tồn kho (tổng từ các lots)
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Giá gốc/đơn vị tính
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Giá bán (giá sau khi áp dụng giảm giá)
    /// </summary>
    public decimal FinalPrice { get; set; }

    /// <summary>
    /// Phần trăm giảm giá (%)
    /// </summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// Giá gợi ý từ AI
    /// </summary>
    public decimal SuggestedPrice { get; set; }

    // ===== THÔNG TIN BỔ SUNG =====
    /// <summary>
    /// Mã barcode
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// Là thực phẩm tươi sống?
    /// </summary>
    public bool IsFreshFood { get; set; }

    /// <summary>
    /// Loại định lượng (Fixed = cố định, Variable = bán theo cân)
    /// </summary>
    public string WeightTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Giá/kg (cho sản phẩm bán theo cân)
    /// </summary>
    public decimal? DefaultPricePerKg { get; set; }

    /// <summary>
    /// Trạng thái sản phẩm
    /// </summary>
    public ProductState Status { get; set; }

    /// <summary>
    /// Tên siêu thị
    /// </summary>
    public string SupermarketName { get; set; } = string.Empty;

    // ===== THÔNG TIN ẢNH =====
    /// <summary>
    /// Ảnh đại diện chính
    /// </summary>
    public string? MainImageUrl { get; set; }

    /// <summary>
    /// Tổng số ảnh
    /// </summary>
    public int TotalImages { get; set; }

    /// <summary>
    /// Danh sách tất cả ảnh
    /// </summary>
    public List<ProductImageDto> ProductImages { get; set; } = new();

    // ===== THÔNG TIN HẠN SỬ DỤNG (Tính toán) =====
    /// <summary>
    /// Số ngày còn lại đến hạn sử dụng
    /// </summary>
    public int? DaysToExpiry { get; set; }

    /// <summary>
    /// Trạng thái hạn sử dụng
    /// </summary>
    public ExpiryStatus? ExpiryStatus { get; set; }

    /// <summary>
    /// Text mô tả trạng thái hạn
    /// VD: "Còn 5 ngày", "Sắp hết hạn", "Quá hạn 2 ngày"
    /// </summary>
    public string? ExpiryStatusText { get; set; }
}

/// <summary>
/// Response cho danh sách sản phẩm với thông tin bán hàng
/// Format: Description | Category | Unit | Qty | Giá gốc | Giá bán | % Giảm | Giá gợi ý
/// </summary>
public class ProductSalesListItemDto
{
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// Mô tả/Tên sản phẩm
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Danh mục/Loại
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Đơn vị tính
    /// </summary>
    public string Unit { get; set; } = string.Empty;
    
    /// <summary>
    /// Số lượng tồn kho
    /// </summary>
    public decimal Quantity { get; set; }
    
    /// <summary>
    /// Giá gốc/đơn vị tính
    /// </summary>
    public decimal OriginalPrice { get; set; }
    
    /// <summary>
    /// Giá bán
    /// </summary>
    public decimal FinalPrice { get; set; }
    
    /// <summary>
    /// % Giảm giá
    /// </summary>
    public decimal DiscountPercent { get; set; }
    
    /// <summary>
    /// Giá gợi ý từ AI
    /// </summary>
    public decimal SuggestedPrice { get; set; }
    
    /// <summary>
    /// Ảnh đại diện
    /// </summary>
    public string? MainImageUrl { get; set; }
    
    /// <summary>
    /// Số ngày còn hạn
    /// </summary>
    public int? DaysToExpiry { get; set; }
    
    /// <summary>
    /// Trạng thái hạn sử dụng
    /// </summary>
    public ExpiryStatus? ExpiryStatus { get; set; }
}
