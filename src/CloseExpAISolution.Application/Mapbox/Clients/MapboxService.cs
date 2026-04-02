using System.Net.Http.Json;
using CloseExpAISolution.Application.Mapbox.Configuration;
using CloseExpAISolution.Application.Mapbox.DTOs;
using CloseExpAISolution.Application.Mapbox.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CloseExpAISolution.Application.Mapbox.Clients;

public class MapboxService : IMapboxService
{
    private readonly HttpClient _httpClient;
    private readonly MapboxSettings _settings;
    private readonly ILogger<MapboxService> _logger;

    public MapboxService(HttpClient httpClient, IOptions<MapboxSettings> settings, ILogger<MapboxService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<GeocodingResultDto?> ForwardGeocodeAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        var encodedAddress = Uri.EscapeDataString(address.Trim());
        var url = $"/search/geocode/v6/forward" +
                  $"?q={encodedAddress}" +
                  $"&country={_settings.CountryCode}" +
                  $"&language={_settings.Language}" +
                  $"&limit=1" +
                  $"&access_token={_settings.AccessToken}";

        var response = await CallMapboxAsync(url, ct);
        return response?.Features.Count > 0
            ? MapToResult(response.Features[0])
            : null;
    }

    public async Task<GeocodingResultDto?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        var url = $"/search/geocode/v6/reverse" +
                  $"?longitude={longitude}" +
                  $"&latitude={latitude}" +
                  $"&language={_settings.Language}" +
                  $"&access_token={_settings.AccessToken}";

        var response = await CallMapboxAsync(url, ct);
        return response?.Features.Count > 0
            ? MapToResult(response.Features[0])
            : null;
    }

    public async Task<IEnumerable<GeocodingResultDto>> SearchAddressAsync(string query, int limit = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<GeocodingResultDto>();

        limit = Math.Clamp(limit, 1, 10);
        var encodedQuery = Uri.EscapeDataString(query.Trim());
        var url = $"/search/geocode/v6/forward" +
                  $"?q={encodedQuery}" +
                  $"&country={_settings.CountryCode}" +
                  $"&language={_settings.Language}" +
                  $"&autocomplete=true" +
                  $"&limit={limit}" +
                  $"&access_token={_settings.AccessToken}";

        var response = await CallMapboxAsync(url, ct);
        if (response?.Features == null || response.Features.Count == 0)
            return Enumerable.Empty<GeocodingResultDto>();

        return response.Features.Select(MapToResult);
    }

    public async Task<double?> GetDrivingDistanceKmAsync(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        CancellationToken ct = default)
    {
        var url = $"/directions/v5/mapbox/driving/{fromLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{fromLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)};{toLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{toLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $"?alternatives=false&geometries=geojson&overview=simplified&access_token={_settings.AccessToken}";

        try
        {
            _logger.LogDebug("Mapbox directions request: {BaseUrl}{Path}", _settings.BaseUrl, SanitizeUrl(url));
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Mapbox Directions returned {StatusCode}: {Error}", (int)response.StatusCode, errorBody);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;
            if (!root.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
                return null;

            var firstRoute = routes[0];
            if (!firstRoute.TryGetProperty("distance", out var distMeters))
                return null;

            return distMeters.GetDouble() / 1000d;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Mapbox driving distance request failed");
            return null;
        }
    }

    #region Private Helpers

    private async Task<MapboxGeocodingResponse?> CallMapboxAsync(string url, CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Mapbox request: {BaseUrl}{Path}", _settings.BaseUrl, SanitizeUrl(url));
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Mapbox API returned {StatusCode}: {Error}", (int)response.StatusCode, errorBody);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<MapboxGeocodingResponse>(cancellationToken: ct);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Mapbox request timed out");
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Mapbox request failed");
            return null;
        }
    }

    private static GeocodingResultDto MapToResult(MapboxFeature feature)
    {
        var props = feature.Properties;
        var coords = props.CoordinatesDetail;
        var geometry = feature.Geometry;

        return new GeocodingResultDto
        {
            Latitude = coords?.Latitude ?? (geometry.Coordinates.Count >= 2 ? geometry.Coordinates[1] : 0),
            Longitude = coords?.Longitude ?? (geometry.Coordinates.Count >= 2 ? geometry.Coordinates[0] : 0),
            FullAddress = props.FullAddress ?? props.PlaceFormatted ?? props.Name,
            PlaceName = props.Context?.Place?.Name,
            Region = props.Context?.Region?.Name,
            District = props.Context?.District?.Name,
            Country = props.Context?.Country?.Name,
            CountryCode = props.Context?.Country?.CountryCode,
            Accuracy = coords?.Accuracy
        };
    }

    private static string SanitizeUrl(string url)
    {
        var tokenIndex = url.IndexOf("access_token=", StringComparison.Ordinal);
        return tokenIndex >= 0 ? url[..tokenIndex] + "access_token=***" : url;
    }

    #endregion
}
