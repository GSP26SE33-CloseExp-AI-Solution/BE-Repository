using CloseExpAISolution.Application.Mapbox.DTOs;

namespace CloseExpAISolution.Application.Mapbox.Interfaces;

/// <summary>
/// Service cho Mapbox Geocoding API v6:
/// Forward geocoding (address → coordinates), Reverse geocoding (coordinates → address),
/// và Address suggestion (autocomplete)
/// </summary>
public interface IMapboxService
{
    /// <summary>
    /// Chuyển đổi địa chỉ text → tọa độ GPS (forward geocoding)
    /// </summary>
    Task<GeocodingResultDto?> ForwardGeocodeAsync(string address, CancellationToken ct = default);

    /// <summary>
    /// Chuyển đổi tọa độ GPS → địa chỉ text (reverse geocoding)
    /// </summary>
    Task<GeocodingResultDto?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default);

    /// <summary>
    /// Gợi ý danh sách địa chỉ (autocomplete) cho frontend
    /// </summary>
    Task<IEnumerable<GeocodingResultDto>> SearchAddressAsync(string query, int limit = 5, CancellationToken ct = default);
}
