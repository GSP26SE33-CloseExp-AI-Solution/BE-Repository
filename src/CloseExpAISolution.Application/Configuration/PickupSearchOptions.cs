namespace CloseExpAISolution.Application.Configuration;

public class PickupSearchOptions
{
    public const string SectionName = "PickupSearch";

    /// <summary>Default search radius in km when the client omits <c>radiusKm</c>.</summary>
    public double DefaultRadiusKm { get; set; } = 5;

    /// <summary>Maximum radius in km the API will accept (input is clamped).</summary>
    public double MaxRadiusKm { get; set; } = 50;
}
