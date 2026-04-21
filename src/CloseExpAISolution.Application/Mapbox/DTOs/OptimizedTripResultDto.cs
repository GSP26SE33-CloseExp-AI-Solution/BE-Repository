namespace CloseExpAISolution.Application.Mapbox.DTOs;

/// <summary>
/// Kết quả tối ưu lộ trình từ Mapbox Optimization v1 (optimized-trips).
/// </summary>
public sealed class OptimizedTripResultDto
{
    /// <summary>
    /// Thứ tự các stop (tính trên danh sách coordinates đã truyền vào, bỏ start-index).
    /// Ví dụ: nếu truyền [start, S1, S2, S3] và Mapbox xếp S2 → S1 → S3,
    /// thì <c>OrderedStopIndices</c> = [2, 1, 3].
    /// </summary>
    public IReadOnlyList<int> OrderedStopIndices { get; init; } = Array.Empty<int>();

    /// <summary>Mapbox polyline6 encoded geometry của trip đầu tiên.</summary>
    public string EncodedPolyline { get; init; } = string.Empty;

    public double DistanceMeters { get; init; }

    public double DurationSeconds { get; init; }
}
