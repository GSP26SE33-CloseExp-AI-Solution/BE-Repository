namespace CloseExpAISolution.Application.DTOs.Request;

public class DeliveryRoutePlanRequestDto
{
    public double? StartLatitude { get; set; }

    public double? StartLongitude { get; set; }

    /// <summary>
    /// Optimize by road distance (<c>distance</c>) or driving time (<c>duration</c>).
    /// </summary>
    public string Metric { get; set; } = "distance";
}
