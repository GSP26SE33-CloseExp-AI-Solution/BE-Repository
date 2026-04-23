using System.Globalization;
using System.Text.Json;
using CloseExpAISolution.Application.Mapbox.Configuration;
using CloseExpAISolution.Application.Mapbox.DTOs;
using CloseExpAISolution.Application.Mapbox.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloseExpAISolution.Application.Mapbox.Clients;

/// <summary>
/// Mapbox Optimization API v1 client (optimized-trips, driving profile).
/// Docs: https://docs.mapbox.com/api/navigation/optimization-v1/
/// </summary>
public class MapboxOptimizationService : IMapboxOptimizationService
{
    public const int MaxCoordinatesPerRequest = 12;

    private readonly HttpClient _httpClient;
    private readonly MapboxSettings _settings;
    private readonly ILogger<MapboxOptimizationService> _logger;

    public MapboxOptimizationService(
        HttpClient httpClient,
        IOptions<MapboxSettings> settings,
        ILogger<MapboxOptimizationService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<OptimizedTripResultDto?> OptimizeDrivingRouteAsync(
        IReadOnlyList<(double Latitude, double Longitude)> coordinates,
        int? startIndex = 0,
        CancellationToken ct = default)
    {
        if (coordinates == null || coordinates.Count < 2)
            return null;

        if (coordinates.Count > MaxCoordinatesPerRequest)
        {
            _logger.LogWarning(
                "Optimization request has {Count} coordinates, exceeds limit {Limit}.",
                coordinates.Count, MaxCoordinatesPerRequest);
            return null;
        }

        var coordPath = string.Join(";", coordinates.Select(c =>
            $"{c.Longitude.ToString(CultureInfo.InvariantCulture)},{c.Latitude.ToString(CultureInfo.InvariantCulture)}"));

        var url = $"/optimized-trips/v1/mapbox/driving/{coordPath}" +
                  $"?geometries=polyline6&overview=full&roundtrip=false" +
                  $"&source={(startIndex is 0 ? "first" : "any")}" +
                  $"&destination=any" +
                  $"&access_token={_settings.AccessToken}";

        try
        {
            _logger.LogDebug("Mapbox optimization request: {BaseUrl}{Path}", _settings.BaseUrl, SanitizeUrl(url));
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Mapbox Optimization returned {StatusCode}: {Error}",
                    (int)response.StatusCode, errorBody);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            if (!root.TryGetProperty("code", out var codeEl) ||
                !string.Equals(codeEl.GetString(), "Ok", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Mapbox Optimization code not Ok: {Json}", root.GetRawText());
                return null;
            }

            if (!root.TryGetProperty("trips", out var tripsEl) ||
                tripsEl.ValueKind != JsonValueKind.Array ||
                tripsEl.GetArrayLength() == 0)
                return null;

            var firstTrip = tripsEl[0];
            if (!firstTrip.TryGetProperty("geometry", out var geomEl) || geomEl.ValueKind != JsonValueKind.String)
                return null;
            if (!firstTrip.TryGetProperty("distance", out var distEl) ||
                !firstTrip.TryGetProperty("duration", out var durEl))
                return null;

            var encoded = geomEl.GetString() ?? string.Empty;
            if (string.IsNullOrEmpty(encoded))
                return null;

            // waypoints[i] = { waypoint_index, trips_index } với i = index trong input coordinates.
            // waypoint_index = vị trí trong chuỗi ghé thăm do Mapbox tối ưu.
            if (!root.TryGetProperty("waypoints", out var wpEl) ||
                wpEl.ValueKind != JsonValueKind.Array ||
                wpEl.GetArrayLength() != coordinates.Count)
                return null;

            var startIdx = startIndex ?? 0;
            var ordered = new List<(int InputIndex, int VisitOrder)>(coordinates.Count);
            for (var i = 0; i < wpEl.GetArrayLength(); i++)
            {
                var w = wpEl[i];
                if (!w.TryGetProperty("waypoint_index", out var wIdxEl) || !wIdxEl.TryGetInt32(out var visitOrder))
                    return null;
                ordered.Add((i, visitOrder));
            }

            var orderedStops = ordered
                .Where(x => x.InputIndex != startIdx)
                .OrderBy(x => x.VisitOrder)
                .Select(x => x.InputIndex)
                .ToList();

            return new OptimizedTripResultDto
            {
                OrderedStopIndices = orderedStops,
                EncodedPolyline = encoded,
                DistanceMeters = distEl.GetDouble(),
                DurationSeconds = durEl.GetDouble()
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Mapbox optimization request failed");
            return null;
        }
    }

    private static string SanitizeUrl(string url)
    {
        var tokenIndex = url.IndexOf("access_token=", StringComparison.Ordinal);
        return tokenIndex >= 0 ? url[..tokenIndex] + "access_token=***" : url;
    }
}
