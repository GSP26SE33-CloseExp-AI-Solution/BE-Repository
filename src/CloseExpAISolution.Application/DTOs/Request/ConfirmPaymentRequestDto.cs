namespace CloseExpAISolution.Application.DTOs.Request;

public class ConfirmPaymentRequestDto
{
    /// <summary>PayOS numeric order code from the payment link.</summary>
    public string OrderCode { get; set; } = string.Empty;
}
