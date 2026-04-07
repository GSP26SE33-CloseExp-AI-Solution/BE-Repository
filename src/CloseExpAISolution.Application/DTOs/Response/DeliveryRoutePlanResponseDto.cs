namespace CloseExpAISolution.Application.DTOs.Response;

public class DeliveryRoutePlanResponseDto
{
    public IReadOnlyList<Guid> OrderedOrderIds { get; set; } = Array.Empty<Guid>();

    public double TotalDistanceKm { get; set; }

    public double TotalDurationMinutes { get; set; }

    /// <summary>Mapbox polyline6; empty when no segment to draw.</summary>
    public string EncodedPolyline { get; set; } = string.Empty;

    public string PolylineEncoding { get; set; } = "polyline6";

    public string Metric { get; set; } = string.Empty;

    /// <summary>Orders in the group without coordinates (excluded from optimization).</summary>
    public IReadOnlyList<Guid> SkippedOrderIds { get; set; } = Array.Empty<Guid>();
}
