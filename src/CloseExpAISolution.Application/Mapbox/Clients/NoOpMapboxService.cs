using CloseExpAISolution.Application.Mapbox.DTOs;
using CloseExpAISolution.Application.Mapbox.Interfaces;

namespace CloseExpAISolution.Application.Mapbox.Clients;

public class NoOpMapboxService : IMapboxService
{
    public Task<GeocodingResultDto?> ForwardGeocodeAsync(string address, CancellationToken ct = default) =>
        Task.FromResult<GeocodingResultDto?>(null);

    public Task<GeocodingResultDto?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default) =>
        Task.FromResult<GeocodingResultDto?>(null);

    public Task<IEnumerable<GeocodingResultDto>> SearchAddressAsync(string query, int limit = 5, CancellationToken ct = default) =>
        Task.FromResult(Enumerable.Empty<GeocodingResultDto>());

    public Task<double?> GetDrivingDistanceKmAsync(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        CancellationToken ct = default) =>
        Task.FromResult<double?>(null);

    public Task<DrivingMatrixResultDto?> GetDrivingMatrixAsync(
        IReadOnlyList<(double Latitude, double Longitude)> coordinates,
        CancellationToken ct = default) =>
        Task.FromResult<DrivingMatrixResultDto?>(null);

    public Task<DrivingRouteGeometryDto?> GetDrivingRoutePolylineAsync(
        IReadOnlyList<(double Latitude, double Longitude)> waypoints,
        CancellationToken ct = default) =>
        Task.FromResult<DrivingRouteGeometryDto?>(null);
}
