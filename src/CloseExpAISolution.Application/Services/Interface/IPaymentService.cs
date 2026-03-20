using CloseExpAISolution.Application.DTOs.Response;
using PayOS.Models.Webhooks;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IPaymentService
{
    /// <summary>Creates a PayOS payment link for the given order; returns checkout URL.</summary>
    Task<string> CreatePaymentLinkAsync(
        Guid userId,
        Guid orderId,
        string? returnUrl,
        string? cancelUrl,
        CancellationToken cancellationToken = default);

    Task HandleWebhookAsync(Webhook webhook, CancellationToken cancellationToken = default);

    /// <summary>Polls PayOS for payment link status; if paid/settled, updates <see cref="Domain.Entities.Transaction"/> and order.</summary>
    Task<PaymentConfirmResult> ConfirmPaymentAsync(long payOsOrderCode, CancellationToken cancellationToken = default);
}
