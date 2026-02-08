namespace CloseExpAISolution.Domain.Entities;

public class Pricing
{
    public Guid PricingId { get; set; }
    public Guid ProductId { get; set; }
    public decimal BasePrice { get; set; }
    public string BaseUnit { get; set; } = string.Empty;
    public string Currency { get; set; } = "VND";
    public decimal? SalePrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public string? PricedBy { get; set; } // chuyển sang bảng đề xuất giá riêng
    public DateTime? PricedAt { get; set; } // chuyển sang bảng đề xuất giá riêng
    public float PricingConfidence { get; set; } // chuyển sang bảng đề xuất giá riêng
    public string? PricingReasons { get; set; } // chuyển sang bảng đề xuất giá riêng
    public decimal OriginalUnitPrice { get; set; }
    public decimal SuggestedUnitPrice { get; set; } // chuyển sang bảng đề xuất giá riêng
    public decimal FinalUnitPrice { get; set; }     
    public Product? Product { get; set; }
}
