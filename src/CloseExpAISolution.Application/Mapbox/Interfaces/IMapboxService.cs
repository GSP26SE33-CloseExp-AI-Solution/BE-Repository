using CloseExpAISolution.Application.Mapbox.DTOs;

namespace CloseExpAISolution.Application.Mapbox.Interfaces;

public interface IMapboxService
{
    Task<GeocodingResultDto?> ForwardGeocodeAsync(string address, CancellationToken ct = default);

    Task<GeocodingResultDto?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default);

    Task<IEnumerable<GeocodingResultDto>> SearchAddressAsync(string query, int limit = 5, CancellationToken ct = default);

    Task<double?> GetDrivingDistanceKmAsync(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        CancellationToken ct = default);
}
