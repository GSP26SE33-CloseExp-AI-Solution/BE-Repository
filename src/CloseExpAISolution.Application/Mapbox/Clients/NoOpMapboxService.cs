using CloseExpAISolution.Application.Mapbox.DTOs;
using CloseExpAISolution.Application.Mapbox.Interfaces;

namespace CloseExpAISolution.Application.Mapbox.Clients;

/// <summary>
/// No-op implementation when Mapbox AccessToken is not configured. Allows the app to run without Mapbox; geocoding returns null/empty.
/// </summary>
public class NoOpMapboxService : IMapboxService
{
    public Task<GeocodingResultDto?> ForwardGeocodeAsync(string address, CancellationToken ct = default) =>
        Task.FromResult<GeocodingResultDto?>(null);

    public Task<GeocodingResultDto?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default) =>
        Task.FromResult<GeocodingResultDto?>(null);

    public Task<IEnumerable<GeocodingResultDto>> SearchAddressAsync(string query, int limit = 5, CancellationToken ct = default) =>
        Task.FromResult(Enumerable.Empty<GeocodingResultDto>());
}
