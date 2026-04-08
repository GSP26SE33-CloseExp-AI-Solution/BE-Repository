using CloseExpAISolution.Application.Email.Interfaces;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CloseExpAISolution.Application.Email.Jobs;

[DisallowConcurrentExecution]
public class ProcessRefundEmailOutboxJob : IJob
{
    public const int MaxAttempts = 10;
    public const int BatchSize = 50;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProcessRefundEmailOutboxJob> _logger;

    public ProcessRefundEmailOutboxJob(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<ProcessRefundEmailOutboxJob> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var now = DateTime.UtcNow;

        var pending = await _unitOfWork.Repository<RefundEmailOutbox>()
            .AsQueryable()
            .AsNoTracking()
            .Where(x => x.Status == RefundEmailOutboxStatus.Pending
                && x.AttemptCount < MaxAttempts
                && (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now))
            .OrderBy(x => x.CreatedAtUtc)
            .Take(BatchSize)
            .Select(x => x.EmailOutboxId)
            .ToListAsync(ct);

        foreach (var id in pending)
        {
            ct.ThrowIfCancellationRequested();
            await ProcessOneAsync(id, ct);
        }
    }

    private async Task ProcessOneAsync(Guid outboxId, CancellationToken ct)
    {
        var row = await _unitOfWork.Repository<RefundEmailOutbox>()
            .FirstOrDefaultAsync(x => x.EmailOutboxId == outboxId);
        if (row == null || row.Status != RefundEmailOutboxStatus.Pending)
            return;

        var now = DateTime.UtcNow;
        if (row.NextAttemptAtUtc.HasValue && row.NextAttemptAtUtc > now)
            return;

        var refund = await _unitOfWork.Repository<Refund>()
            .FirstOrDefaultAsync(r => r.RefundId == row.RefundId);
        if (refund == null)
        {
            await MarkDeadLetterAsync(row, "Refund not found", ct);
            return;
        }

        var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(refund.OrderId, ct);
        if (order == null)
        {
            await MarkDeadLetterAsync(row, "Order not found", ct);
            return;
        }

        var email = order.User?.Email?.Trim();
        if (string.IsNullOrEmpty(email))
        {
            await MarkDeadLetterAsync(row, "No customer email", ct);
            return;
        }

        var (subject, body) = RefundEmailComposer.Compose(refund, order, row.Kind);

        try
        {
            await _emailService.SendEmailAsync(email, subject, body, ct);
            row.Status = RefundEmailOutboxStatus.Sent;
            row.SentAtUtc = DateTime.UtcNow;
            row.LastError = null;
            row.NextAttemptAtUtc = null;
            _unitOfWork.Repository<RefundEmailOutbox>().Update(row);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Refund outbox email sent. outboxId={OutboxId}, refundId={RefundId}, kind={Kind}",
                outboxId, refund.RefundId, row.Kind);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            row.AttemptCount++;
            var msg = ex.Message.Length > 4000 ? ex.Message[..4000] : ex.Message;
            row.LastError = msg;
            if (row.AttemptCount >= MaxAttempts)
            {
                row.Status = RefundEmailOutboxStatus.DeadLetter;
                row.NextAttemptAtUtc = null;
            }
            else
            {
                var delaySec = Math.Min(30 * Math.Pow(2, row.AttemptCount - 1), 3600);
                row.NextAttemptAtUtc = DateTime.UtcNow + TimeSpan.FromSeconds(delaySec);
            }

            _unitOfWork.Repository<RefundEmailOutbox>().Update(row);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogWarning(ex, "Refund outbox send failed (attempt {Attempt}). outboxId={OutboxId}",
                row.AttemptCount, outboxId);
        }
    }

    private async Task MarkDeadLetterAsync(RefundEmailOutbox row, string reason, CancellationToken ct)
    {
        row.Status = RefundEmailOutboxStatus.DeadLetter;
        row.LastError = reason.Length > 4000 ? reason[..4000] : reason;
        row.NextAttemptAtUtc = null;
        _unitOfWork.Repository<RefundEmailOutbox>().Update(row);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
