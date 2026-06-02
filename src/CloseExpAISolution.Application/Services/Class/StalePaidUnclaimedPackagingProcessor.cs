using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Application.Services.Fulfillment;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

public sealed class StalePaidUnclaimedPackagingProcessor : IStalePaidUnclaimedPackagingProcessor
{
    public const int BatchSize = 100;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProviders _services;
    private readonly OrderStockQuantityHelper _orderStockQuantityHelper;
    private readonly ILogger<StalePaidUnclaimedPackagingProcessor> _logger;

    public StalePaidUnclaimedPackagingProcessor(
        IUnitOfWork unitOfWork,
        IServiceProviders services,
        OrderStockQuantityHelper orderStockQuantityHelper,
        ILogger<StalePaidUnclaimedPackagingProcessor> logger)
    {
        _unitOfWork = unitOfWork;
        _services = services;
        _orderStockQuantityHelper = orderStockQuantityHelper;
        _logger = logger;
    }

    public async Task<int> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var leadMinutes = await GetLeadMinutesAsync(cancellationToken);
        var candidateIds = await _unitOfWork.Repository<Order>()
            .AsQueryable()
            .AsNoTracking()
            .Where(o => o.Status == OrderState.Paid)
            .OrderBy(o => o.UpdatedAt)
            .Select(o => o.OrderId)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (candidateIds.Count == 0)
            return 0;

        var processed = 0;
        foreach (var orderId in candidateIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (await ProcessOneAsync(orderId, leadMinutes, cancellationToken))
                    processed++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "StalePaidUnclaimedPackagingProcessor failed for order {OrderId}", orderId);
            }
        }

        return processed;
    }

    private async Task<bool> ProcessOneAsync(Guid orderId, int leadMinutes, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        Guid? pendingRefundId = null;
        try
        {
            var order = await _unitOfWork.Repository<Order>()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null || order.Status != OrderState.Paid)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            var items = (await _unitOfWork.Repository<OrderItem>()
                .FindAsync(i => i.OrderId == orderId))
                .ToList();
            if (items.Count == 0)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            // Only auto-fail when the order is still untouched by packaging flow.
            if (items.Any(i => i.PackagingStatus != PackagingState.Pending))
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            var placeholderUserId = await ResolveUnassignedPackagingActorUserIdAsync(cancellationToken);
            var packRecords = (await _unitOfWork.Repository<OrderPackaging>()
                .FindAsync(r => r.OrderId == orderId && r.OrderItemId != null))
                .ToList();
            var recordByItemId = packRecords
                .Where(r => r.OrderItemId.HasValue)
                .ToDictionary(r => r.OrderItemId!.Value);

            if (items.Any(i =>
                    !recordByItemId.TryGetValue(i.OrderItemId, out var record)
                    || record.Status != PackagingState.Pending
                    || record.UserId != placeholderUserId))
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            var lotIds = items.Select(i => i.LotId).Distinct().ToList();
            var lots = (await _unitOfWork.Repository<StockLot>()
                .FindAsync(l => lotIds.Contains(l.LotId)))
                .ToList();
            var lotById = lots.ToDictionary(l => l.LotId);

            if (lotById.Count != lotIds.Count)
            {
                _logger.LogWarning(
                    "Order {OrderId} has missing StockLot rows while checking stale paid packaging.",
                    orderId);
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            var earliestExpiry = lots.Min(l => l.ExpiryDate);
            var autoFailAt = earliestExpiry.AddMinutes(-leadMinutes);
            var now = DateTime.UtcNow;
            if (now < autoFailAt)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            var requiredByLot = await _orderStockQuantityHelper.ComputeRequiredStockQuantityByLotAsync(
                items,
                cancellationToken);
            foreach (var (lotId, requiredQuantity) in requiredByLot)
            {
                if (!lotById.TryGetValue(lotId, out var lot))
                    continue;
                lot.Quantity += requiredQuantity;
                lot.UpdatedAt = now;
                _unitOfWork.Repository<StockLot>().Update(lot);
            }

            var failReason = BuildAutoFailReason(earliestExpiry, leadMinutes);
            foreach (var item in items)
            {
                item.PackagingStatus = PackagingState.Failed;
                item.PackagingFailedReason = failReason;
                item.DeliveryStatus = null;
                _unitOfWork.Repository<OrderItem>().Update(item);
            }

            order.Status = OrderState.Failed;
            order.UpdatedAt = now;
            _unitOfWork.Repository<Order>().Update(order);

            await _unitOfWork.Repository<OrderStatusLog>().AddAsync(new OrderStatusLog
            {
                LogId = Guid.NewGuid(),
                OrderId = orderId,
                FromStatus = OrderState.Paid,
                ToStatus = OrderState.Failed,
                ChangedBy = "system:auto-unclaimed-packaging",
                Note = failReason,
                ChangedAt = now
            });

            var paidTx = (await _unitOfWork.Repository<Transaction>()
                    .FindAsync(t => t.OrderId == orderId && t.PaymentStatus == PaymentState.Paid))
                .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                .FirstOrDefault();

            if (paidTx != null)
            {
                var existingRefundTotal = (await _unitOfWork.Repository<Refund>().FindAsync(r =>
                        r.TransactionId == paidTx.TransactionId && r.Status != RefundState.Rejected))
                    .Sum(r => r.Amount);
                var refundable = RefundAmountCalculator.ComputeRefundable(paidTx.Amount, existingRefundTotal);
                if (refundable > 0)
                {
                    var createdRefund = await _services.RefundService.CreateAsync(
                        new CreateRefundRequestDto
                        {
                            OrderId = orderId,
                            TransactionId = paidTx.TransactionId,
                            Amount = refundable,
                            Reason = failReason,
                            OrderItemIds = items.Select(i => i.OrderItemId).ToList()
                        },
                        cancellationToken);
                    pendingRefundId = createdRefund.RefundId;
                }
            }
            else
            {
                _logger.LogWarning(
                    "Order {OrderId} reached stale paid-packaging timeout but no paid transaction found.",
                    orderId);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();

            if (pendingRefundId.HasValue)
            {
                await TryEnqueuePendingRefundEmailAsync(pendingRefundId.Value, cancellationToken);
            }

            _logger.LogWarning(
                "Auto-failed paid order {OrderId} because no packaging staff accepted in time. earliestExpiry={EarliestExpiry:u}, leadMinutes={LeadMinutes}, refundId={RefundId}",
                orderId,
                earliestExpiry,
                leadMinutes,
                pendingRefundId);

            return true;
        }
        catch
        {
            if (_unitOfWork.HasActiveTransaction)
                await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task TryEnqueuePendingRefundEmailAsync(Guid refundId, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _unitOfWork.Repository<RefundEmailOutbox>()
                .FindAsync(o => o.RefundId == refundId
                    && o.Kind == RefundNotificationKind.Pending
                    && (o.Status == RefundEmailOutboxStatus.Pending
                        || o.Status == RefundEmailOutboxStatus.Sent));
            if (existing.Any())
                return;

            await _services.RefundService.EnqueueRefundCustomerNotificationAsync(
                refundId,
                RefundNotificationKind.Pending,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to enqueue pending refund email for stale paid order. refundId={RefundId}",
                refundId);
        }
    }

    private async Task<int> GetLeadMinutesAsync(CancellationToken cancellationToken)
    {
        var config = await _unitOfWork.Repository<SystemConfig>()
            .FirstOrDefaultAsync(x => x.ConfigKey == SystemConfigKeys.OrderPaidUnclaimedPackagingLeadMinutes);

        if (config == null)
            throw new InvalidOperationException(
                $"Thiếu SystemConfig '{SystemConfigKeys.OrderPaidUnclaimedPackagingLeadMinutes}'. Vui lòng cấu hình phút chốt tự động fail cho đơn Paid chưa nhận đóng gói.");

        if (!int.TryParse(config.ConfigValue, out var minutes) || minutes <= 0)
            throw new InvalidOperationException(
                $"SystemConfig '{SystemConfigKeys.OrderPaidUnclaimedPackagingLeadMinutes}' không hợp lệ. Giá trị phải là số nguyên dương.");

        return minutes;
    }

    private async Task<Guid> ResolveUnassignedPackagingActorUserIdAsync(CancellationToken cancellationToken)
    {
        var cfg = await _unitOfWork.Repository<SystemConfig>()
            .FirstOrDefaultAsync(c => c.ConfigKey == SystemConfigKeys.PackagingUnassignedActorUserId);

        if (cfg != null && Guid.TryParse(cfg.ConfigValue, out var configured) && configured != Guid.Empty)
            return configured;

        var admin = (await _unitOfWork.Repository<User>()
                .FindAsync(u => u.RoleId == (int)RoleUser.Admin && u.Status == UserState.Active))
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefault();

        if (admin != null)
            return admin.UserId;

        throw new InvalidOperationException(
            "Chưa cấu hình PACKAGING_UNASSIGNED_ACTOR_USER_ID và không tìm thấy tài khoản Admin để kiểm tra trạng thái nhận đóng gói.");
    }

    private static string BuildAutoFailReason(DateTime earliestExpiry, int leadMinutes)
    {
        return
            $"Tự động fail: đơn đã thanh toán nhưng chưa có nhân viên đóng gói nhận xử lý trước {leadMinutes} phút so với hạn dùng gần nhất ({earliestExpiry:yyyy-MM-dd HH:mm} UTC).";
    }
}
