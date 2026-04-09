namespace CloseExpAISolution.Application.DTOs.Request;

/// <summary>
/// Body for PUT /api/Orders/{id}/canceled — customer or staff must supply a non-empty cancellation reason.
/// </summary>
public class CancelOrderRequestDto
{
    public string Reason { get; set; } = string.Empty;
}
