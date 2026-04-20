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

public class StaleReadyToShipRefundProcessor : IStaleReadyToShipRefundProcessor
{
    public const int BatchSize = 100;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProviders _services;
    private readonly ILogger<StaleReadyToShipRefundProcessor> _logger;

    public StaleReadyToShipRefundProcessor(
        IUnitOfWork unitOfWork,
        IServiceProviders services,
        ILogger<StaleReadyToShipRefundProcessor> logger)
    {
        _unitOfWork = unitOfWork;
        _services = services;
        _logger = logger;
    }

    public async Task<int> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var maxWaitMinutes = await GetMaxWaitMinutesAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var cutoffChangedAt = now.AddMinutes(-maxWaitMinutes);

        // Chọn sơ bộ: các đơn RTS đã từng có log vào ReadyToShip trước cutoff.
        // Trong ProcessOneAsync sẽ lấy log RTS mới nhất và re-check bằng policy
        // để loại các đơn mới re-enter RTS (chưa đủ tuổi).
        var rtsLogQuery = _unitOfWork.Repository<OrderStatusLog>()
            .AsQueryable()
            .Where(l => l.ToStatus == OrderState.ReadyToShip && l.ChangedAt <= cutoffChangedAt);

        var candidateIds = await _unitOfWork.Repository<Order>()
            .AsQueryable()
            .AsNoTracking()
            .Where(o => o.Status == OrderState.ReadyToShip)
            .Where(o => rtsLogQuery.Any(l => l.OrderId == o.OrderId))
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
                if (await ProcessOneAsync(orderId, maxWaitMinutes, cancellationToken))
                    processed++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "StaleReadyToShipRefundProcessor failed for order {OrderId}", orderId);
            }
        }

        return processed;
    }

    private async Task<bool> ProcessOneAsync(Guid orderId, int maxWaitMinutes, CancellationToken cancellationToken)
    {
        // Re-load và re-check trong một transaction để chống race với các luồng
        // khác (packaging, delivery) có thể đổi trạng thái đơn.
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var order = await _unitOfWork.Repository<Order>()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null || order.Status != OrderState.ReadyToShip)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            var latestRtsLog = (await _unitOfWork.Repository<OrderStatusLog>()
                    .FindAsync(l => l.OrderId == orderId && l.ToStatus == OrderState.ReadyToShip))
                .OrderByDescending(l => l.ChangedAt)
                .FirstOrDefault();

            if (latestRtsLog == null)
            {
                _logger.LogWarning("Order {OrderId} is ReadyToShip but has no status log entry.", orderId);
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            var now = DateTime.UtcNow;
            if (!StaleReadyToShipPolicy.IsDueForRefund(latestRtsLog.ChangedAt, now, maxWaitMinutes))
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            // Idempotent guard: đã có refund không bị Rejected cho đơn này → bỏ qua.
            var existingRefunds = await _unitOfWork.Repository<Refund>()
                .FindAsync(r => r.OrderId == orderId && r.Status != RefundState.Rejected);
            if (existingRefunds.Any())
            {
                _logger.LogInformation(
                    "Order {OrderId} already has an active refund; skipping auto-refund.", orderId);
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            var paidTransactions = (await _unitOfWork.Repository<Transaction>()
                    .FindAsync(t => t.OrderId == orderId && t.PaymentStatus == PaymentState.Paid))
                .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                .ToList();
            var paidTx = paidTransactions.FirstOrDefault();
            if (paidTx == null)
            {
                _logger.LogWarning(
                    "Order {OrderId} has no paid transaction; cannot auto-refund.", orderId);
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            var alreadyRefunded = (await _unitOfWork.Repository<Refund>().FindAsync(r =>
                    r.TransactionId == paidTx.TransactionId && r.Status != RefundState.Rejected))
                .Sum(r => r.Amount);
            var refundable = RefundAmountCalculator.ComputeRefundable(paidTx.Amount, alreadyRefunded);
            if (refundable <= 0)
            {
                _logger.LogInformation(
                    "Order {OrderId}: no refundable balance on transaction {TxId}.",
                    orderId, paidTx.TransactionId);
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            var reason = StaleReadyToShipPolicy.BuildRefundReason(latestRtsLog.ChangedAt, maxWaitMinutes);

            await _services.RefundService.CreateAsync(
                new CreateRefundRequestDto
                {
                    OrderId = orderId,
                    TransactionId = paidTx.TransactionId,
                    Amount = refundable,
                    Reason = reason
                },
                cancellationToken);

            await _services.OrderService.UpdateStatusAsync(orderId, OrderState.Refunded, reason, cancellationToken);

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "Auto-refunded stale ReadyToShip order {OrderId}. rtsAt={RtsAt}, maxWaitMinutes={N}, amount={Amount}",
                orderId, latestRtsLog.ChangedAt, maxWaitMinutes, refundable);
            return true;
        }
        catch
        {
            if (_unitOfWork.HasActiveTransaction)
                await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task<int> GetMaxWaitMinutesAsync(CancellationToken cancellationToken)
    {
        var config = await _unitOfWork.Repository<SystemConfig>()
            .FirstOrDefaultAsync(x => x.ConfigKey == SystemConfigKeys.OrderReadyToShipMaxWaitMinutes);

        if (config == null)
            throw new InvalidOperationException(
                $"Thiếu SystemConfig '{SystemConfigKeys.OrderReadyToShipMaxWaitMinutes}'. Vui lòng cấu hình số phút tối đa cho trạng thái ReadyToShip.");

        if (!int.TryParse(config.ConfigValue, out var minutes) || minutes <= 0)
            throw new InvalidOperationException(
                $"SystemConfig '{SystemConfigKeys.OrderReadyToShipMaxWaitMinutes}' không hợp lệ. Giá trị phải là số nguyên dương.");

        return minutes;
    }
}
