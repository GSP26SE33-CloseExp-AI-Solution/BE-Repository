using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Payment;
using CloseExpAISolution.Application.Policies;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
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
    private const string CancelWindowConfigKey = "ORDER_CANCEL_WINDOW_MINUTES_AFTER_PAID";

    /// <summary>PayOS CreatePaymentLink validates description length (error: "Mô tả tối đa 25 kí tự").</summary>
    private const int PayOsMaxDescriptionLength = 25;

    private readonly IUnitOfWork _unitOfWork;
    private readonly PayOsSettings _settings;
    private readonly ILogger<PaymentService> _logger;
    private readonly PayOSClient _client;
    private readonly StackExchange.Redis.IConnectionMultiplexer? _redis;
    private readonly IServiceProviders? _services;

    public PaymentService(
        IUnitOfWork unitOfWork,
        IOptions<PayOsSettings> options,
        ILogger<PaymentService> logger,
        IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _settings = options.Value;
        _logger = logger;
        _redis = serviceProvider.GetService<StackExchange.Redis.IConnectionMultiplexer>();
        _services = serviceProvider.GetService<IServiceProviders>();
        _client = new PayOSClient(new PayOSOptions
        {
            ClientId = _settings.ClientId,
            ApiKey = _settings.ApiKey,
            ChecksumKey = _settings.ChecksumKey
        });
    }

    public async Task<CreatePaymentLinkResponseDto> CreatePaymentLinkAsync(
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

        var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order not found: {orderId}");

        if (order.UserId != userId)
            throw new UnauthorizedAccessException("You do not own this order.");

        if (order.Status != OrderState.Pending)
            throw new InvalidOperationException("Only pending orders can create payment links.");

        if (IsOrderAlreadyPaid(order.Status))
            throw new InvalidOperationException(AlreadyPaidMessage);

        EnsureOrderLotsStillOrderable(order);

        var amountVnd = (long)Math.Round(order.FinalAmount, MidpointRounding.AwayFromZero);
        if (amountVnd < MinimumAmount)
            throw new InvalidOperationException($"Order amount must be at least {MinimumAmount} (after rounding).");

        if (string.IsNullOrWhiteSpace(returnUrl) || string.IsNullOrWhiteSpace(cancelUrl))
            throw new InvalidOperationException("ReturnUrl and CancelUrl are required in the request body.");

        var payOsOrderCode = await GenerateUniquePayOSOrderCodeAsync(cancellationToken);
        var description = BuildPayOsDescription(order.OrderCode);

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
            return new CreatePaymentLinkResponseDto
            {
                CheckoutUrl = link.CheckoutUrl ?? string.Empty,
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                TotalAmount = order.TotalAmount,
                DiscountAmount = order.DiscountAmount,
                DeliveryFee = order.DeliveryFee,
                SystemUsageFeeAmount = order.SystemUsageFeeAmount,
                FinalAmount = order.FinalAmount
            };
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

        var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(transaction.OrderId, cancellationToken);
        if (order != null)
        {
            await ApplyPaidTransitionSafelyAsync(order, cancellationToken);
        }

        _unitOfWork.Repository<Transaction>().Update(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (order != null
            && order.Status is OrderState.Paid
            or OrderState.ReadyToShip
            or OrderState.DeliveredWaitConfirm
            or OrderState.Completed)
        {
            await TryClearCartAsync(order.UserId, cancellationToken);
        }

        return PaymentConfirmResult.Ok();
    }

    private static bool IsPaymentLinkSettled(PaymentLink link)
    {
        if (link.Status == PaymentLinkStatus.Paid)
            return true;

        if (link.Amount <= 0)
            return false;

        if (link.AmountPaid < link.Amount)
            return false;

        // Treat as settled when PayOS has collected the full amount unless it was cancelled, failed, or expired.
        return link.Status is not (PaymentLinkStatus.Cancelled or PaymentLinkStatus.Failed or PaymentLinkStatus.Expired);
    }

    private static bool IsOrderAlreadyPaid(OrderState orderStatus)
    {
        return orderStatus is OrderState.Paid
            or OrderState.ReadyToShip
            or OrderState.DeliveredWaitConfirm
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

        Order? order = null;
        if (success)
        {
            order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(transaction.OrderId, cancellationToken);
            if (order != null)
            {
                if (order.Status == OrderState.Canceled)
                    await AutoRefundCanceledOrderAsync(order, transaction, cancellationToken);
                else
                    await ApplyPaidTransitionSafelyAsync(order, cancellationToken);
            }
        }

        _unitOfWork.Repository<Transaction>().Update(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (success
            && order != null
            && order.Status is OrderState.Paid
            or OrderState.ReadyToShip
            or OrderState.DeliveredWaitConfirm
            or OrderState.Completed)
        {
            await TryClearCartAsync(order.UserId, cancellationToken);
        }
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

    private async Task ApplyPaidTransitionSafelyAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.Status is not (OrderState.Pending or OrderState.Paid))
        {
            _logger.LogInformation(
                "Skip paid transition for order {OrderId} because status is {Status}",
                order.OrderId,
                order.Status);
            return;
        }

        var now = DateTime.UtcNow;
        var changed = false;

        if (order.Status == OrderState.Pending)
        {
            var consumed = await TryConsumeStockForOrderAsync(order, cancellationToken);
            if (!consumed)
            {
                // Payment is successful, but we cannot fulfill due to insufficient inventory.
                order.Status = OrderState.Failed;
                order.UpdatedAt = now;
                _unitOfWork.OrderRepository.Update(order);
                return;
            }

            order.Status = OrderState.Paid;
            changed = true;
        }

        if (order.Status == OrderState.Paid && !order.CancelDeadline.HasValue)
        {
            var windowMinutes = await GetCancelWindowMinutesAfterPaidAsync(cancellationToken);
            order.CancelDeadline = now.AddMinutes(windowMinutes);
            changed = true;
        }

        if (changed)
        {
            order.UpdatedAt = now;
            _unitOfWork.OrderRepository.Update(order);
        }
    }

    private async Task<bool> TryConsumeStockForOrderAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.OrderItems == null || order.OrderItems.Count == 0)
            return true;

        var requiredByLot = order.OrderItems
            .GroupBy(oi => oi.LotId)
            .Select(g => new
            {
                LotId = g.Key,
                RequiredQuantity = (decimal)g.Sum(x => x.Quantity)
            })
            .ToList();

        var lotIds = requiredByLot.Select(x => x.LotId).ToList();

        var lots = await _unitOfWork.Repository<StockLot>()
            .FindAsync(l => lotIds.Contains(l.LotId));

        var lotById = lots.ToDictionary(l => l.LotId);
        var now = DateTime.UtcNow;
        var cutoffReached = DailyExpiryOrderingPolicy.IsOrderCutoffReached(now);

        foreach (var req in requiredByLot)
        {
            if (!lotById.TryGetValue(req.LotId, out var lot))
                return false;

            if (lot.Status != ProductState.Published)
                return false;

            if (lot.ExpiryDate <= now)
                return false;

            if (cutoffReached && DailyExpiryOrderingPolicy.IsExpiringInVietnamToday(lot.ExpiryDate, now))
                return false;

            if (lot.Quantity < req.RequiredQuantity)
                return false;
        }

        foreach (var req in requiredByLot)
        {
            var lot = lotById[req.LotId];
            lot.Quantity -= req.RequiredQuantity;
            lot.UpdatedAt = now;
            _unitOfWork.Repository<StockLot>().Update(lot);
        }

        return true;
    }

    private void EnsureOrderLotsStillOrderable(Order order)
    {
        if (order.OrderItems == null || order.OrderItems.Count == 0)
            throw new InvalidOperationException("Order has no items to pay.");

        var now = DateTime.UtcNow;
        var cutoffReached = DailyExpiryOrderingPolicy.IsOrderCutoffReached(now);
        var requiredByLot = order.OrderItems
            .GroupBy(oi => oi.LotId)
            .ToDictionary(g => g.Key, g => (decimal)g.Sum(x => x.Quantity));

        foreach (var req in requiredByLot)
        {
            var lot = order.OrderItems
                .Select(oi => oi.StockLot)
                .FirstOrDefault(l => l != null && l.LotId == req.Key);

            if (lot == null)
                throw new InvalidOperationException($"StockLot {req.Key} không tồn tại hoặc đã bị xóa.");

            if (lot.Status != ProductState.Published || lot.Quantity <= 0 || lot.ExpiryDate <= now)
                throw new InvalidOperationException($"StockLot {req.Key} không còn khả dụng để thanh toán.");

            if (cutoffReached && DailyExpiryOrderingPolicy.IsExpiringInVietnamToday(lot.ExpiryDate, now))
                throw new InvalidOperationException("Sau 21:00, không thể thanh toán đơn có lô hàng hết hạn trong ngày.");

            if (lot.Quantity < req.Value)
                throw new InvalidOperationException($"StockLot {req.Key} không đủ số lượng để thanh toán.");
        }
    }

    private async Task AutoRefundCanceledOrderAsync(
        Order order,
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        if (_services == null)
            throw new InvalidOperationException("ServiceProviders is not available for refund flow.");

        var hasActiveRefund = (await _unitOfWork.Repository<Refund>()
            .FindAsync(r => r.TransactionId == transaction.TransactionId && r.Status != RefundState.Rejected))
            .Any();

        if (!hasActiveRefund)
        {
            await _services.RefundService.CreateAsync(
                new CreateRefundRequestDto
                {
                    OrderId = order.OrderId,
                    TransactionId = transaction.TransactionId,
                    Amount = transaction.Amount,
                    Reason = "Auto refund: late payment received after cutoff cancellation."
                },
                cancellationToken);
        }

        if (order.Status != OrderState.Refunded)
        {
            await _services.OrderService.UpdateStatusAsync(
                order.OrderId,
                OrderState.Refunded,
                "Auto-refunded after canceled order received successful payment.",
                cancellationToken);
        }
    }

    private async Task<int> GetCancelWindowMinutesAfterPaidAsync(CancellationToken cancellationToken)
    {
        var config = await _unitOfWork.Repository<SystemConfig>()
            .FirstOrDefaultAsync(x => x.ConfigKey == CancelWindowConfigKey);

        if (config == null)
            throw new InvalidOperationException(
                $"Thiếu SystemConfig '{CancelWindowConfigKey}'. Vui lòng cấu hình số phút cho phép hủy sau khi thanh toán.");

        if (!int.TryParse(config.ConfigValue, out var minutes) || minutes <= 0)
            throw new InvalidOperationException(
                $"SystemConfig '{CancelWindowConfigKey}' không hợp lệ. Giá trị phải là số nguyên dương.");

        return minutes;
    }

    /// <summary>
    /// Dùng mã đơn nội bộ làm mô tả PayOS; cắt chuỗi nếu vượt giới hạn 25 ký tự của API.
    /// </summary>
    private static string BuildPayOsDescription(string orderCode)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
            return "CloseExp";

        var trimmed = orderCode.Trim();
        return trimmed.Length <= PayOsMaxDescriptionLength
            ? trimmed
            : trimmed[..PayOsMaxDescriptionLength];
    }

    private async Task TryClearCartAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_redis == null)
            return;

        // Must match CartService key format: "cart:{userId:D}"
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"cart:{userId:D}");
    }

    public void Dispose() => _client.Dispose();
}
