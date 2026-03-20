namespace CloseExpAISolution.Application.DTOs.Request;

/// <summary>Request body for creating a PayOS checkout link (domain: order to pay, not a single product SKU).</summary>
public class CreatePaymentRequestDto
{
    public Guid OrderId { get; set; }

    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}
