
namespace CloseExpAISolution.Application.Mapbox.Configuration;

/// <summary>
/// Configuration settings for Mapbox Geocoding API integration
/// </summary>
public class MapboxSettings
{
    public const string SectionName = "Mapbox";

    public string AccessToken { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.mapbox.com";
    public int TimeoutSeconds { get; set; } = 10;
    public int RetryCount { get; set; } = 2;
    public int RetryDelayMs { get; set; } = 500;
    public string CountryCode { get; set; } = "vn";
    public string Language { get; set; } = "vi";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AccessToken))
            throw new InvalidOperationException("Mapbox AccessToken is required. Get one at https://console.mapbox.com/");
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException("Mapbox BaseUrl is required.");
    }
}