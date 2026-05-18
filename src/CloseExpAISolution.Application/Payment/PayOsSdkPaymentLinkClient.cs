using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;

namespace CloseExpAISolution.Application.Payment;

public sealed class PayOsSdkPaymentLinkClient : IPayOsPaymentLinkClient
{
    private readonly PayOSClient _client;

    public PayOsSdkPaymentLinkClient(IOptions<PayOsSettings> options)
    {
        var s = options.Value;
        _client = new PayOSClient(new PayOSOptions
        {
            ClientId = s.ClientId,
            ApiKey = s.ApiKey,
            ChecksumKey = s.ChecksumKey
        });
    }

    public Task<CreatePaymentLinkResponse> CreateAsync(CreatePaymentLinkRequest request) =>
        _client.PaymentRequests.CreateAsync(request);

    public Task<PaymentLink> GetAsync(long payOsOrderCode) =>
        _client.PaymentRequests.GetAsync(payOsOrderCode);
}
