using System.Text.Json;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Fulfillment;

/// <summary>
/// Shared refund creation for item-level fulfillment failures (packaging / delivery).
/// </summary>
public static class FulfillmentFailureRefundHelper
{
    public static string BuildFailureNote(string category, string failureReason, string? notes)
    {
        var reason = failureReason.Trim();
        var extra = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        return extra == null ? $"{category}: {reason}" : $"{category}: {reason} | {extra}";
    }

    public static async Task<Guid?> TryCreateRefundForFailedOrderItemsAsync(
        IUnitOfWork unitOfWork,
        IRefundService refundService,
        ILogger logger,
        Guid orderId,
        IReadOnlyList<OrderItem> failedItems,
        string reason,
        CancellationToken cancellationToken,
        bool throwIfNoPaidTransaction = false)
    {
        if (failedItems.Count == 0)
            return null;

        var alreadyRefundedItemIds = await GetAlreadyRefundedOrderItemIdsAsync(
            unitOfWork,
            orderId,
            cancellationToken);

        var itemsToRefund = failedItems
            .Where(i => !alreadyRefundedItemIds.Contains(i.OrderItemId))
            .ToList();

        if (itemsToRefund.Count == 0)
        {
            logger.LogInformation(
                "Order {OrderId}: all failed lines already covered by an active refund; skipping refund create.",
                orderId);
            return null;
        }

        var refundAmount = itemsToRefund.Sum(i => i.TotalPrice);
        if (refundAmount <= 0)
            return null;

        var transactions = (await unitOfWork.Repository<Transaction>()
                .FindAsync(t => t.OrderId == orderId && t.PaymentStatus == PaymentState.Paid))
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .ToList();

        var paidTx = transactions.FirstOrDefault();
        if (paidTx == null)
        {
            if (throwIfNoPaidTransaction)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy giao dịch thanh toán thành công, không thể tạo yêu cầu hoàn tiền.");
            }

            logger.LogWarning(
                "Order {OrderId}: no paid transaction found; skipping refund for failed items.",
                orderId);
            return null;
        }

        var existingRefundTotal = (await unitOfWork.Repository<Refund>().FindAsync(r =>
                r.TransactionId == paidTx.TransactionId && r.Status != RefundState.Rejected))
            .Sum(r => r.Amount);

        var refundable = RefundAmountCalculator.ComputeRefundable(paidTx.Amount, existingRefundTotal);
        if (refundable <= 0)
        {
            logger.LogWarning(
                "Order {OrderId}: no refundable balance left on transaction {TxId}.",
                orderId,
                paidTx.TransactionId);
            return null;
        }

        var amount = Math.Min(refundAmount, refundable);
        if (amount <= 0)
            return null;

        var refundReason = reason.Length > 2000 ? reason[..2000] : reason;
        var refundedItemIds = itemsToRefund.Select(i => i.OrderItemId).ToList();

        var created = await refundService.CreateAsync(
            new CreateRefundRequestDto
            {
                OrderId = orderId,
                TransactionId = paidTx.TransactionId,
                Amount = amount,
                Reason = refundReason,
                OrderItemIds = refundedItemIds
            },
            cancellationToken);

        return created.RefundId;
    }

    private static async Task<HashSet<Guid>> GetAlreadyRefundedOrderItemIdsAsync(
        IUnitOfWork unitOfWork,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var activeRefunds = await unitOfWork.Repository<Refund>()
            .FindAsync(r => r.OrderId == orderId && r.Status != RefundState.Rejected);

        var ids = new HashSet<Guid>();
        foreach (var refund in activeRefunds)
        {
            foreach (var id in ParseRefundItemIds(refund.RefundedOrderItemIdsJson) ?? Array.Empty<Guid>())
                ids.Add(id);
        }

        return ids;
    }

    private static IReadOnlyList<Guid>? ParseRefundItemIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
