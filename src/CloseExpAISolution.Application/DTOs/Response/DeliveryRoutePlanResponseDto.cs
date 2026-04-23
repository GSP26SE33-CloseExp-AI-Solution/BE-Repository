namespace CloseExpAISolution.Application.DTOs.Response;

public class DeliveryRoutePlanResponseDto
{
    public IReadOnlyList<Guid> OrderedOrderIds { get; set; } = Array.Empty<Guid>();

    /// <summary>Tổng (Leg A + Leg B) distance, đơn vị km.</summary>
    public double TotalDistanceKm { get; set; }

    /// <summary>Tổng (Leg A + Leg B) duration, đơn vị phút.</summary>
    public double TotalDurationMinutes { get; set; }

    /// <summary>
    /// Polyline chặng giao (Leg B: supermarket -> khách) để tương thích các client cũ
    /// chỉ đọc field EncodedPolyline. Đặt rỗng nếu không có stop.
    /// </summary>
    public string EncodedPolyline { get; set; } = string.Empty;

    public string PolylineEncoding { get; set; } = "polyline6";

    public string Metric { get; set; } = string.Empty;

    /// <summary>Orders in the group without coordinates (excluded from optimization).</summary>
    public IReadOnlyList<Guid> SkippedOrderIds { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Chặng A: Vị trí shipper (GPS hiện tại) → siêu thị (pickup). Null khi thiếu toạ độ siêu thị.
    /// </summary>
    public RouteLegDto? PickupLeg { get; set; }

    /// <summary>
    /// Chặng B: Siêu thị → khách hàng (đã tối ưu thứ tự stop). Null khi bucket rỗng.
    /// </summary>
    public RouteLegDto? DeliveryLeg { get; set; }
}

/// <summary>
/// Chi tiết một chặng trong lộ trình two-leg.
/// </summary>
public class RouteLegDto
{
    public string Kind { get; set; } = string.Empty;

    public double DistanceKm { get; set; }

    public double DurationMinutes { get; set; }

    public string EncodedPolyline { get; set; } = string.Empty;

    public string PolylineEncoding { get; set; } = "polyline6";

    /// <summary>Chiến lược routing dùng cho chặng này, ví dụ: backend / mapbox-optimization / directions.</summary>
    public string StrategyUsed { get; set; } = string.Empty;

    public RouteLegEndpointDto? From { get; set; }

    public RouteLegEndpointDto? To { get; set; }
}

public class RouteLegEndpointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Label { get; set; }
}
