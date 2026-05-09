using CloseExpAISolution.Application.DTOs.Response;
using PayOS.Models.Webhooks;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IPaymentService
{
    Task<CreatePaymentLinkResponseDto> CreatePaymentLinkAsync(
        Guid userId,
        Guid orderId,
        string? returnUrl,
        string? cancelUrl,
        CancellationToken cancellationToken = default);

    Task HandleWebhookAsync(Webhook webhook, CancellationToken cancellationToken = default);

    Task<PaymentConfirmResult> ConfirmPaymentAsync(long payOsOrderCode, CancellationToken cancellationToken = default);
}
