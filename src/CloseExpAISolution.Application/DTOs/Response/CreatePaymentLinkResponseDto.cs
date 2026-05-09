namespace CloseExpAISolution.Application.DTOs.Response;

public class CreatePaymentLinkResponseDto
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal SystemUsageFeeAmount { get; set; }
    public decimal FinalAmount { get; set; }
}
