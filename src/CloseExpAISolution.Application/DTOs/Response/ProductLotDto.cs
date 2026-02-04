using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Application.DTOs.Request;

namespace CloseExpAISolution.Application.DTOs.Response;

/// <summary>
/// DTO chi tiết cho một lô sản phẩm (ProductLot) với thông tin hạn sử dụng
/// </summary>
public class ProductLotDetailDto
{
    // Thông tin lô hàng
    public Guid LotId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime ManufactureDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal Weight { get; set; }
    public string Status { get; set; } = string.Empty;

    // Thông tin đơn vị
    public Guid UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;

    // Giá của lô
    public decimal OriginalUnitPrice { get; set; }
    public decimal SuggestedUnitPrice { get; set; }
    public decimal FinalUnitPrice { get; set; }

    // Thông tin sản phẩm
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public ProductWeightType WeightType { get; set; }
    public string WeightTypeName { get; set; } = string.Empty;
    public decimal? DefaultPricePerKg { get; set; }

    // Thông tin siêu thị
    public Guid SupermarketId { get; set; }
    public string SupermarketName { get; set; } = string.Empty;

    // Thông tin ảnh sản phẩm
    /// <summary>
    /// Ảnh đại diện chính của sản phẩm
    /// </summary>
    public string? MainImageUrl { get; set; }

    /// <summary>
    /// Tổng số ảnh của sản phẩm
    /// </summary>
    public int TotalImages { get; set; }

    /// <summary>
    /// Danh sách tất cả ảnh sản phẩm
    /// </summary>
    public List<ProductImageDto> ProductImages { get; set; } = new();

    // Thông tin hạn sử dụng (tính toán)
    public ExpiryStatus ExpiryStatus { get; set; }

    /// <summary>
    /// Số ngày còn lại hoặc đã quá hạn (âm nếu quá hạn)
    /// </summary>
    public int DaysRemaining { get; set; }

    /// <summary>
    /// Số giờ còn lại (chỉ dùng cho trạng thái Today)
    /// </summary>
    public int? HoursRemaining { get; set; }

    /// <summary>
    /// Text mô tả trạng thái hạn sử dụng
    /// VD: "Còn 12 giờ", "Sắp hết hạn (2 ngày)", "Quá hạn 3 ngày"
    /// </summary>
    public string ExpiryStatusText { get; set; } = string.Empty;

    // Thông tin thành phần & dinh dưỡng
    /// <summary>
    /// Thành phần nguyên liệu của sản phẩm
    /// </summary>
    public string? Ingredients { get; set; }

    /// <summary>
    /// Thông tin dinh dưỡng (parsed từ JSON)
    /// </summary>
    public Dictionary<string, string>? NutritionFacts { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO để lọc ProductLot theo siêu thị và trạng thái hạn
/// </summary>
public class ProductLotFilterDto
{
    public Guid? SupermarketId { get; set; }

    /// <summary>
    /// Lọc theo trạng thái hạn sử dụng
    /// </summary>
    public ExpiryStatus? ExpiryStatus { get; set; }

    /// <summary>
    /// Lọc theo loại định lượng
    /// </summary>
    public ProductWeightType? WeightType { get; set; }

    /// <summary>
    /// Chỉ lấy sản phẩm tươi
    /// </summary>
    public bool? IsFreshFood { get; set; }

    /// <summary>
    /// Tìm kiếm theo tên sản phẩm
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Lọc theo category
    /// </summary>
    public string? Category { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
