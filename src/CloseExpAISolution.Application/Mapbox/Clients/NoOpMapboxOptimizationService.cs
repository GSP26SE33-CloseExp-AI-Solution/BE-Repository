using CloseExpAISolution.Application.Mapbox.DTOs;
using CloseExpAISolution.Application.Mapbox.Interfaces;

namespace CloseExpAISolution.Application.Mapbox.Clients;

/// <summary>
/// Fallback khi không có Mapbox token; trả về null để HybridRoutingStrategy dùng backend.
/// </summary>
public class NoOpMapboxOptimizationService : IMapboxOptimizationService
{
    public Task<OptimizedTripResultDto?> OptimizeDrivingRouteAsync(
        IReadOnlyList<(double Latitude, double Longitude)> coordinates,
        int? startIndex = 0,
        CancellationToken ct = default) =>
        Task.FromResult<OptimizedTripResultDto?>(null);
}
