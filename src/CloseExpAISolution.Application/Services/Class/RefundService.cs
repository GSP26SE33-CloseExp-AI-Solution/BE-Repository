using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class RefundService : IRefundService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RefundService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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

    public async Task<RefundResponseDto?> GetByIdAsync(Guid refundId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<Refund>().FirstOrDefaultAsync(r => r.RefundId == refundId);
        return entity == null ? null : _mapper.Map<RefundResponseDto>(entity);
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
    }

    /// <summary>Pending → Approved | Rejected; Approved → Completed. Terminal: Rejected, Completed.</summary>
    private static bool CanTransition(RefundState from, RefundState to) => (from, to) switch
    {
        (RefundState.Pending, RefundState.Approved) => true,
        (RefundState.Pending, RefundState.Rejected) => true,
        (RefundState.Approved, RefundState.Completed) => true,
        _ => false
    };
}
