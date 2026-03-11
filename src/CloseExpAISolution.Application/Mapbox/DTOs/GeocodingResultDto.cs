namespace CloseExpAISolution.Application.Mapbox.DTOs;

/// <summary>
/// Internal DTO chứa kết quả geocoding đã parse từ Mapbox response
/// </summary>
public class GeocodingResultDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string FullAddress { get; set; } = string.Empty;
    public string? PlaceName { get; set; }
    public string? Region { get; set; }
    public string? District { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? Accuracy { get; set; }
}
