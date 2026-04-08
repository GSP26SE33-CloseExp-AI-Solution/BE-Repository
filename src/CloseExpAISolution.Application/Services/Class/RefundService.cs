using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Email.Jobs;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CloseExpAISolution.Application.Services.Class;

public class RefundService : IRefundService
{
    private static readonly HashSet<string> AllowedRefundEmailEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "created",
        "approved",
        "rejected",
        "completed"
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ISchedulerFactory schedulerFactory,
        ILogger<RefundService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    public async Task<(IEnumerable<RefundResponseDto> Items, int TotalCount)> GetAllAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = (await _unitOfWork.Repository<Refund>().GetAllAsync()).ToList();
        all.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
        var total = all.Count;
        var page = all
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => _mapper.Map<RefundResponseDto>(r))
            .ToList();
        return (page, total);
    }

    public async Task<(IEnumerable<RefundResponseDto> Items, int TotalCount)> GetByUserAsync(
        Guid userId, Guid? orderId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var orderIds = (await _unitOfWork.Repository<Order>()
                .FindAsync(o => o.UserId == userId && (!orderId.HasValue || o.OrderId == orderId.Value)))
            .Select(o => o.OrderId)
            .ToHashSet();

        if (orderIds.Count == 0)
            return (Array.Empty<RefundResponseDto>(), 0);

        var all = (await _unitOfWork.Repository<Refund>()
                .FindAsync(r => orderIds.Contains(r.OrderId)))
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var total = all.Count;
        var page = all
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => _mapper.Map<RefundResponseDto>(r))
            .ToList();
        return (page, total);
    }

    public async Task<RefundResponseDto?> GetByIdAsync(Guid refundId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<Refund>().FirstOrDefaultAsync(r => r.RefundId == refundId);
        return entity == null ? null : _mapper.Map<RefundResponseDto>(entity);
    }

    public async Task<RefundResponseDto?> GetByIdForUserAsync(Guid refundId, Guid userId, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Repository<Refund>().FirstOrDefaultAsync(r => r.RefundId == refundId);
        if (refund == null)
            return null;

        var order = await _unitOfWork.Repository<Order>().FirstOrDefaultAsync(o => o.OrderId == refund.OrderId);
        if (order == null || order.UserId != userId)
            return null;

        return _mapper.Map<RefundResponseDto>(refund);
    }

    public async Task<RefundResponseDto> CreateAsync(CreateRefundRequestDto request, CancellationToken cancellationToken = default)
    {
        _ = await _unitOfWork.OrderRepository.GetByOrderIdAsync(request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order not found: {request.OrderId}");

        var transaction = await _unitOfWork.Repository<Transaction>()
            .FirstOrDefaultAsync(t => t.TransactionId == request.TransactionId);
        if (transaction == null)
            throw new KeyNotFoundException($"Transaction not found: {request.TransactionId}");

        if (transaction.OrderId != request.OrderId)
            throw new InvalidOperationException("Transaction does not belong to the specified order.");

        if (request.Amount <= 0)
            throw new InvalidOperationException("Refund amount must be greater than zero.");

        if (request.Amount > transaction.Amount)
            throw new InvalidOperationException("Refund amount cannot exceed the transaction amount.");

        var existingTotal = (await _unitOfWork.Repository<Refund>().FindAsync(r =>
                r.TransactionId == request.TransactionId && r.Status != RefundState.Rejected))
            .Sum(r => r.Amount);

        if (existingTotal + request.Amount > transaction.Amount)
            throw new InvalidOperationException("Total refunds for this transaction would exceed the paid amount.");

        var refund = new Refund
        {
            RefundId = Guid.NewGuid(),
            OrderId = request.OrderId,
            TransactionId = request.TransactionId,
            Amount = request.Amount,
            Reason = request.Reason.Trim(),
            Status = RefundState.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Refund>().AddAsync(refund);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await TryScheduleRefundStatusEmailJobAsync(refund.RefundId, "created");

        return _mapper.Map<RefundResponseDto>(refund);
    }

    public async Task UpdateStatusAsync(
        Guid refundId,
        RefundState newStatus,
        string? processedBy,
        CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Repository<Refund>().FirstOrDefaultAsync(r => r.RefundId == refundId)
            ?? throw new KeyNotFoundException($"Refund not found: {refundId}");

        if (refund.Status == newStatus)
            return;

        if (!CanTransition(refund.Status, newStatus))
            throw new InvalidOperationException(
                $"Cannot change refund status from {refund.Status} to {newStatus}.");

        refund.Status = newStatus;
        if (newStatus is RefundState.Approved or RefundState.Rejected or RefundState.Completed)
        {
            refund.ProcessedAt = DateTime.UtcNow;
            refund.ProcessedBy = processedBy;
        }

        _unitOfWork.Repository<Refund>().Update(refund);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await TryScheduleRefundStatusEmailJobAsync(refund.RefundId, newStatus.ToString());
    }

    // Refund status is intentionally one-way to avoid invalid rollback states.
    private static bool CanTransition(RefundState from, RefundState to) => (from, to) switch
    {
        (RefundState.Pending, RefundState.Approved) => true,
        (RefundState.Pending, RefundState.Rejected) => true,
        (RefundState.Approved, RefundState.Completed) => true,
        _ => false
    };

    private async Task TryScheduleRefundStatusEmailJobAsync(Guid refundId, string eventName)
    {
        var safeEvent = string.IsNullOrWhiteSpace(eventName) ? "updated" : eventName.Trim().ToLowerInvariant();
        if (!AllowedRefundEmailEvents.Contains(safeEvent))
        {
            _logger.LogWarning("Skip refund email job: unsupported event '{EventName}' for refundId={RefundId}", safeEvent, refundId);
            return;
        }

        var jobKey = new JobKey($"SendRefundStatusEmailJob:{refundId}:{safeEvent}", "refund-email");
        var triggerKey = new TriggerKey($"SendRefundStatusEmailJobTrigger:{refundId}:{safeEvent}", "refund-email");

        var jobDetail = JobBuilder.Create<SendRefundStatusEmailJob>()
            .WithIdentity(jobKey)
            .UsingJobData("refundId", refundId.ToString())
            .UsingJobData("eventName", safeEvent)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .StartNow()
            .Build();

        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.ScheduleJob(jobDetail, trigger);
        }
        catch (ObjectAlreadyExistsException)
        {
            _logger.LogInformation("SendRefundStatusEmailJob already scheduled. refundId={RefundId}, event={EventName}",
                refundId, safeEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule SendRefundStatusEmailJob. refundId={RefundId}, event={EventName}",
                refundId, safeEvent);
        }
    }
}
