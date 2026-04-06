namespace CloseExpAISolution.Application.Mapbox.DTOs;

/// <summary>
/// All-pairs driving costs from Mapbox Matrix API (meters / seconds).
/// </summary>
public sealed class DrivingMatrixResultDto
{
    /// <summary>Row/column count (start at index 0 + stops).</summary>
    public int Size { get; init; }

    /// <summary>Distance in meters; null if no route.</summary>
    public double?[,] DistancesMeters { get; init; } = null!;

    /// <summary>Duration in seconds; null if no route.</summary>
    public double?[,] DurationsSeconds { get; init; } = null!;
}
