using System.Text.Json.Serialization;

namespace CloseExpAISolution.Application.Mapbox.DTOs;

public class MapboxGeocodingResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("features")]
    public List<MapboxFeature> Features { get; set; } = new();
}

public class MapboxFeature
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("geometry")]
    public MapboxGeometry Geometry { get; set; } = new();

    [JsonPropertyName("properties")]
    public MapboxProperties Properties { get; set; } = new();
}

public class MapboxGeometry
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("coordinates")]
    public List<double> Coordinates { get; set; } = new();
}

public class MapboxProperties
{
    [JsonPropertyName("mapbox_id")]
    public string MapboxId { get; set; } = string.Empty;

    [JsonPropertyName("feature_type")]
    public string FeatureType { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("name_preferred")]
    public string? NamePreferred { get; set; }

    [JsonPropertyName("place_formatted")]
    public string? PlaceFormatted { get; set; }

    [JsonPropertyName("full_address")]
    public string? FullAddress { get; set; }

    [JsonPropertyName("coordinates")]
    public MapboxCoordinatesDetail? CoordinatesDetail { get; set; }

    [JsonPropertyName("context")]
    public MapboxContext? Context { get; set; }
}

public class MapboxCoordinatesDetail
{
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("accuracy")]
    public string? Accuracy { get; set; }
}

public class MapboxContext
{
    [JsonPropertyName("place")]
    public MapboxContextItem? Place { get; set; }

    [JsonPropertyName("region")]
    public MapboxContextItem? Region { get; set; }

    [JsonPropertyName("country")]
    public MapboxCountryContext? Country { get; set; }

    [JsonPropertyName("district")]
    public MapboxContextItem? District { get; set; }

    [JsonPropertyName("neighborhood")]
    public MapboxContextItem? Neighborhood { get; set; }

    [JsonPropertyName("street")]
    public MapboxContextItem? Street { get; set; }

    [JsonPropertyName("address")]
    public MapboxAddressContext? Address { get; set; }
}

public class MapboxContextItem
{
    [JsonPropertyName("mapbox_id")]
    public string? MapboxId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class MapboxCountryContext : MapboxContextItem
{
    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("country_code_alpha_3")]
    public string? CountryCodeAlpha3 { get; set; }
}

public class MapboxAddressContext : MapboxContextItem
{
    [JsonPropertyName("address_number")]
    public string? AddressNumber { get; set; }

    [JsonPropertyName("street_name")]
    public string? StreetName { get; set; }
}
