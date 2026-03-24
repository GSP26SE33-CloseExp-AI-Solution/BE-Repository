using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Payment;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace CloseExpAISolution.Application.Services.Class;

public sealed class PaymentService : IPaymentService, IDisposable
{
    private const string PayOSMethod = "PayOS";
    private const long MinimumAmount = 1;
    private const string AlreadyPaidMessage = "This order has already been paid.";

    private readonly IUnitOfWork _unitOfWork;
    private readonly PayOsSettings _settings;
    private readonly ILogger<PaymentService> _logger;
    private readonly PayOSClient _client;

    public PaymentService(
        IUnitOfWork unitOfWork,
        IOptions<PayOsSettings> options,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _settings = options.Value;
        _logger = logger;
        _client = new PayOSClient(new PayOSOptions
        {
            ClientId = _settings.ClientId,
            ApiKey = _settings.ApiKey,
            ChecksumKey = _settings.ChecksumKey
        });
    }

    public async Task<string> CreatePaymentLinkAsync(
        Guid userId,
        Guid orderId,
        string? returnUrl,
        string? cancelUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId)
            || string.IsNullOrWhiteSpace(_settings.ApiKey)
            || string.IsNullOrWhiteSpace(_settings.ChecksumKey))
            throw new InvalidOperationException("PayOS is not configured. Set PayOsSettings:ClientId, ApiKey, and ChecksumKey.");

        var order = await _unitOfWork.OrderRepository.GetByOrderIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order not found: {orderId}");

        if (order.UserId != userId)
            throw new UnauthorizedAccessException("You do not own this order.");

        if (IsOrderAlreadyPaid(order.Status))
            throw new InvalidOperationException(AlreadyPaidMessage);

        var amountVnd = (long)Math.Round(order.FinalAmount, MidpointRounding.AwayFromZero);
        if (amountVnd < MinimumAmount)
            throw new InvalidOperationException($"Order amount must be at least {MinimumAmount} (after rounding).");

        if (string.IsNullOrWhiteSpace(returnUrl) || string.IsNullOrWhiteSpace(cancelUrl))
            throw new InvalidOperationException("ReturnUrl and CancelUrl are required in the request body.");

        var payOsOrderCode = await GenerateUniquePayOSOrderCodeAsync(cancellationToken);
        var description = $"Pay order {order.OrderCode}";

        var tx = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            OrderId = order.OrderId,
            Amount = order.FinalAmount,
            PaymentMethod = PayOSMethod,
            PaymentStatus = PaymentState.Pending,
            CreatedAt = DateTime.UtcNow,
            PayOSOrderCode = payOsOrderCode
        };

        await _unitOfWork.Repository<Transaction>().AddAsync(tx);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createRequest = new CreatePaymentLinkRequest
        {
            OrderCode = payOsOrderCode,
            Amount = amountVnd,
            Description = description,
            ReturnUrl = returnUrl!,
            CancelUrl = cancelUrl!
        };

        try
        {
            var link = await _client.PaymentRequests.CreateAsync(createRequest);
            tx.PayOSPaymentLinkId = link.PaymentLinkId;
            tx.CheckoutUrl = link.CheckoutUrl;
            tx.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Transaction>().Update(tx);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return link.CheckoutUrl ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS CreateAsync failed for order {OrderId}", order.OrderId);
            tx.PaymentStatus = PaymentState.Failed;
            tx.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Transaction>().Update(tx);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task HandleWebhookAsync(Webhook webhook, CancellationToken cancellationToken = default)
    {
        WebhookData data;
        try
        {
            data = await _client.Webhooks.VerifyAsync(webhook);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PayOS webhook verification failed");
            throw;
        }

        var transaction = await _unitOfWork.Repository<Transaction>()
            .FirstOrDefaultAsync(t => t.PayOSOrderCode == data.OrderCode);

        if (transaction == null)
        {
            _logger.LogWarning("PayOS webhook: no transaction for OrderCode {OrderCode}", data.OrderCode);
            return;
        }

        await ApplyPaidStateFromWebhookAsync(transaction, webhook.Success, cancellationToken);
    }

    public async Task<PaymentConfirmResult> ConfirmPaymentAsync(long payOsOrderCode, CancellationToken cancellationToken = default)
    {
        PaymentLink link;
        try
        {
            link = await _client.PaymentRequests.GetAsync(payOsOrderCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PayOS GetAsync failed for order code {Code}", payOsOrderCode);
            return PaymentConfirmResult.PayOsFailure(
                "Could not load payment status from PayOS. Check credentials, network, and that the orderCode exists.");
        }

        if (!IsPaymentLinkSettled(link))
        {
            var statusStr = link.Status.ToString();
            _logger.LogInformation(
                "PayOS link {Code} not settled yet: Status={Status}, AmountPaid={AmountPaid}, Amount={Amount}",
                payOsOrderCode, statusStr, link.AmountPaid, link.Amount);
            return PaymentConfirmResult.NotPaidYet(statusStr, link.AmountPaid, link.Amount);
        }

        var transaction = await _unitOfWork.Repository<Transaction>()
            .FirstOrDefaultAsync(t => t.PayOSOrderCode == payOsOrderCode);
        if (transaction == null)
        {
            _logger.LogWarning("Confirm: PayOS reports settled payment but no local Transaction row for code {Code}", payOsOrderCode);
            return PaymentConfirmResult.MissingTransaction(payOsOrderCode);
        }

        if (transaction.PaymentStatus == PaymentState.Paid)
            return PaymentConfirmResult.Ok();

        transaction.PaymentStatus = PaymentState.Paid;
        transaction.UpdatedAt = DateTime.UtcNow;

        var order = await _unitOfWork.OrderRepository.GetByOrderIdAsync(transaction.OrderId, cancellationToken);
        if (order != null && order.Status != OrderState.PaidProcessing)
        {
            order.Status = OrderState.PaidProcessing;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.OrderRepository.Update(order);
        }

        _unitOfWork.Repository<Transaction>().Update(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return PaymentConfirmResult.Ok();
    }

    /// <summary>
    /// PayOS may leave status as Processing briefly after checkout while AmountPaid already matches Amount.
    /// </summary>
    private static bool IsPaymentLinkSettled(PaymentLink link)
    {
        if (link.Status == PaymentLinkStatus.Paid)
            return true;

        if (link.Amount <= 0)
            return false;

        if (link.AmountPaid < link.Amount)
            return false;

        // Fully collected — treat as success unless the link was cancelled / failed / expired.
        return link.Status is not (PaymentLinkStatus.Cancelled or PaymentLinkStatus.Failed or PaymentLinkStatus.Expired);
    }

    private static bool IsOrderAlreadyPaid(OrderState orderStatus)
    {
        return orderStatus is OrderState.PaidProcessing
            or OrderState.Ready_To_Ship
            or OrderState.Delivered_Wait_Confirm
            or OrderState.Completed
            or OrderState.Refunded;
    }

    private async Task ApplyPaidStateFromWebhookAsync(
        Transaction transaction,
        bool success,
        CancellationToken cancellationToken)
    {
        if (transaction.PaymentStatus == PaymentState.Paid)
            return;

        transaction.PaymentStatus = success ? PaymentState.Paid : PaymentState.Failed;
        transaction.UpdatedAt = DateTime.UtcNow;

        if (success)
        {
            var order = await _unitOfWork.OrderRepository.GetByOrderIdAsync(transaction.OrderId, cancellationToken);
            if (order != null && order.Status != OrderState.PaidProcessing)
            {
                order.Status = OrderState.PaidProcessing;
                order.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.OrderRepository.Update(order);
            }
        }

        _unitOfWork.Repository<Transaction>().Update(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<long> GenerateUniquePayOSOrderCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var candidate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000L + Random.Shared.Next(0, 1000);
            if (candidate <= 0)
                continue;

            var exists = await _unitOfWork.Repository<Transaction>()
                .ExistsAsync(t => t.PayOSOrderCode == candidate);
            if (!exists)
                return candidate;
        }

        throw new InvalidOperationException("Could not generate a unique PayOS order code.");
    }

    public void Dispose() => _client.Dispose();
}
