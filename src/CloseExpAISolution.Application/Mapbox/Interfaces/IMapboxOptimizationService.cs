using CloseExpAISolution.Application.Mapbox.DTOs;

namespace CloseExpAISolution.Application.Mapbox.Interfaces;

/// <summary>
/// Mapbox Optimization API v1 (optimized-trips). Giới hạn tối đa 12 toạ độ / request.
/// </summary>
public interface IMapboxOptimizationService
{
    /// <summary>
    /// Tối ưu thứ tự ghé các điểm (không lặp điểm đầu), metric mặc định là duration.
    /// </summary>
    /// <param name="coordinates">
    /// Danh sách toạ độ (lat, lng). Index 0 là start, các index 1..n là stop cần sắp xếp.
    /// </param>
    /// <param name="startIndex">
    /// Index điểm xuất phát trong <paramref name="coordinates"/>. Hiện chỉ hỗ trợ 0 (first).
    /// </param>
    /// <returns>Null khi không gọi được API hoặc response không hợp lệ.</returns>
    Task<OptimizedTripResultDto?> OptimizeDrivingRouteAsync(
        IReadOnlyList<(double Latitude, double Longitude)> coordinates,
        int? startIndex = 0,
        CancellationToken ct = default);
}
