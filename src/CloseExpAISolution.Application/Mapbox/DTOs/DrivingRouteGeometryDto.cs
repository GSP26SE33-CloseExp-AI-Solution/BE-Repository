namespace CloseExpAISolution.Application.Mapbox.DTOs;

/// <summary>
/// Single driving route through ordered waypoints (Mapbox Directions).
/// </summary>
public sealed class DrivingRouteGeometryDto
{
    /// <summary>Mapbox polyline6 encoded geometry.</summary>
    public string EncodedPolyline { get; init; } = string.Empty;

    public double DistanceMeters { get; init; }

    public double DurationSeconds { get; init; }
}
