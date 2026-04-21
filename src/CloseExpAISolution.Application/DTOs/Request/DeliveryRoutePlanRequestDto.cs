namespace CloseExpAISolution.Application.DTOs.Request;

public class DeliveryRoutePlanRequestDto
{
    /// <summary>
    /// Vĩ độ GPS hiện tại của shipper. Dùng cho chặng A (pickup) trong lộ trình two-leg.
    /// Nếu không truyền, backend sẽ fallback sang center nhóm, rồi đến stop đầu tiên.
    /// </summary>
    public double? StartLatitude { get; set; }

    /// <summary>
    /// Kinh độ GPS hiện tại của shipper. Phải đi kèm StartLatitude mới có hiệu lực.
    /// </summary>
    public double? StartLongitude { get; set; }

    /// <summary>
    /// Optimize by road distance (<c>distance</c>) or driving time (<c>duration</c>).
    /// Mặc định <c>duration</c> để tối ưu trải nghiệm food-freshness cho Leg B.
    /// </summary>
    public string Metric { get; set; } = "duration";
}
