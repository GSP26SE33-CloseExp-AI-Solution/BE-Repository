namespace CloseExpAISolution.Application.DTOs.Request;

public class CreatePaymentRequestDto
{
    public Guid OrderId { get; set; }

    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}
