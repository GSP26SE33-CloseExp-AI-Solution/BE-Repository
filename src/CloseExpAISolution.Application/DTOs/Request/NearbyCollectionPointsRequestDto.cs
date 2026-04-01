using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class NearbyCollectionPointsRequestDto
{
    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    /// <summary>Radius in km; when null, PickupSearch:DefaultRadiusKm from configuration is used.</summary>
    [Range(0.1, 500)]
    public double? RadiusKm { get; set; }
}
