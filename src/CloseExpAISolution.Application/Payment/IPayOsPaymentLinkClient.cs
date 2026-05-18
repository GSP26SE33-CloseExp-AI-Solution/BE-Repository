using PayOS.Models.V2.PaymentRequests;

namespace CloseExpAISolution.Application.Payment;

/// <summary>Abstraction over PayOS <c>PaymentRequests.CreateAsync</c> and <c>GetAsync</c> for unit testing.</summary>
public interface IPayOsPaymentLinkClient
{
    Task<CreatePaymentLinkResponse> CreateAsync(CreatePaymentLinkRequest request);

    Task<PaymentLink> GetAsync(long payOsOrderCode);
}
