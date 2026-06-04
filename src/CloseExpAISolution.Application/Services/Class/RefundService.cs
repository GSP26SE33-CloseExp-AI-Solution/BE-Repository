using System.Text.Json;
using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Fulfillment;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

public class RefundService : IRefundService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RefundService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<(IEnumerable<RefundResponseDto> Items, int TotalCount)> GetAllAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, pageNumber);
        var safeSize = Math.Clamp(pageSize, 1, 200);

        var q = _unitOfWork.Repository<Refund>()
            .AsQueryable()
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt);

        var total = await q.CountAsync(cancellationToken);
        var page = await q
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync(cancellationToken);

        var dtos = await MapRefundsAsync(page, cancellationToken);
        await EnrichWithCustomerContactAsync(dtos, cancellationToken);
        return (dtos, total);
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
            .ToList();
        var dtos = await MapRefundsAsync(page, cancellationToken);
        await EnrichWithCustomerContactAsync(dtos, cancellationToken);
        return (dtos, total);
    }

    public async Task<RefundResponseDto?> GetByIdAsync(Guid refundId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<Refund>().FirstOrDefaultAsync(r => r.RefundId == refundId);
        if (entity == null)
            return null;

        var dto = await MapRefundAsync(entity, cancellationToken);
        await EnrichWithCustomerContactAsync(new List<RefundResponseDto> { dto }, cancellationToken);
        return dto;
    }

    public async Task<RefundResponseDto?> GetByIdForUserAsync(Guid refundId, Guid userId, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Repository<Refund>().FirstOrDefaultAsync(r => r.RefundId == refundId);
        if (refund == null)
            return null;

        var order = await _unitOfWork.Repository<Order>().FirstOrDefaultAsync(o => o.OrderId == refund.OrderId);
        if (order == null || order.UserId != userId)
            return null;

        var dto = await MapRefundAsync(refund, cancellationToken);
        await EnrichWithCustomerContactAsync(new List<RefundResponseDto> { dto }, cancellationToken);
        return dto;
    }

    public async Task<RefundResponseDto> CreateAsync(CreateRefundRequestDto request, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(request.OrderId, cancellationToken)
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

        IReadOnlyList<Guid>? refundedItemIds = null;
        if (request.OrderItemIds is { Count: > 0 })
        {
            refundedItemIds = request.OrderItemIds.Distinct().ToList();
            var orderItemIdSet = order.OrderItems.Select(oi => oi.OrderItemId).ToHashSet();
            foreach (var id in refundedItemIds)
            {
                if (!orderItemIdSet.Contains(id))
                    throw new InvalidOperationException($"Order item {id} does not belong to this order.");
            }
        }

        var refund = new Refund
        {
            RefundId = Guid.NewGuid(),
            OrderId = request.OrderId,
            TransactionId = request.TransactionId,
            Amount = request.Amount,
            Reason = request.Reason.Trim(),
            RefundedOrderItemIdsJson = refundedItemIds == null ? null : JsonSerializer.Serialize(refundedItemIds),
            Status = RefundState.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Refund>().AddAsync(refund);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!_unitOfWork.HasActiveTransaction)
        {
            try
            {
                await EnqueueRefundCustomerNotificationAsync(refund.RefundId, RefundNotificationKind.Pending, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue pending refund notification email for refund {RefundId}", refund.RefundId);
            }
        }

        var dto = _mapper.Map<RefundResponseDto>(refund);
        RefundDtoEnricher.EnrichRefundResponse(dto, refund, order);
        return dto;
    }

    public async Task EnqueueRefundCustomerNotificationAsync(
        Guid refundId,
        RefundNotificationKind kind,
        CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Repository<Refund>().FirstOrDefaultAsync(r => r.RefundId == refundId);
        if (refund == null)
            return;

        await _unitOfWork.Repository<RefundEmailOutbox>().AddAsync(new RefundEmailOutbox
        {
            EmailOutboxId = Guid.NewGuid(),
            RefundId = refundId,
            Kind = kind,
            Status = RefundEmailOutboxStatus.Pending,
            AttemptCount = 0,
            CreatedAtUtc = DateTime.UtcNow,
            NextAttemptAtUtc = null
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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

        RefundNotificationKind? notifyKind = newStatus switch
        {
            RefundState.Approved => RefundNotificationKind.Approved,
            RefundState.Rejected => RefundNotificationKind.Rejected,
            RefundState.Completed => RefundNotificationKind.Completed,
            _ => null
        };

        if (notifyKind.HasValue)
        {
            try
            {
                await EnqueueRefundCustomerNotificationAsync(refundId, notifyKind.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue refund lifecycle email for refund {RefundId}, kind={Kind}",
                    refundId, notifyKind);
            }
        }
    }

    private async Task<RefundResponseDto> MapRefundAsync(Refund refund, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<RefundResponseDto>(refund);
        var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(refund.OrderId, cancellationToken);
        if (order != null)
            RefundDtoEnricher.EnrichRefundResponse(dto, refund, order);
        return dto;
    }

    private async Task<List<RefundResponseDto>> MapRefundsAsync(
        IReadOnlyList<Refund> refunds,
        CancellationToken cancellationToken)
    {
        if (refunds.Count == 0)
            return new List<RefundResponseDto>();

        var orderIds = refunds.Select(r => r.OrderId).Distinct().ToList();
        var orders = new Dictionary<Guid, Order>();
        foreach (var orderId in orderIds)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken);
            if (order != null)
                orders[orderId] = order;
        }

        var dtos = new List<RefundResponseDto>(refunds.Count);
        foreach (var refund in refunds)
        {
            var dto = _mapper.Map<RefundResponseDto>(refund);
            if (orders.TryGetValue(refund.OrderId, out var order))
                RefundDtoEnricher.EnrichRefundResponse(dto, refund, order);
            dtos.Add(dto);
        }

        return dtos;
    }

    private static bool CanTransition(RefundState from, RefundState to) => (from, to) switch
    {
        (RefundState.Pending, RefundState.Approved) => true,
        (RefundState.Pending, RefundState.Rejected) => true,
        (RefundState.Approved, RefundState.Completed) => true,
        _ => false
    };

    public async Task<(IEnumerable<AdminRefundOrderSummaryDto> Items, int TotalCount)> GetAdminOrdersWithRefundsAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, pageNumber);
        var safeSize = Math.Clamp(pageSize, 1, 100);

        var grouped = await _unitOfWork.Repository<Refund>()
            .AsQueryable()
            .AsNoTracking()
            .GroupBy(r => r.OrderId)
            .Select(g => new
            {
                OrderId = g.Key,
                LastRefundAt = g.Max(r => r.CreatedAt),
                TotalRefundAmount = g.Where(r => r.Status != RefundState.Rejected).Sum(r => r.Amount),
                PendingRefundAmount = g.Where(r => r.Status == RefundState.Pending).Sum(r => r.Amount),
                RefundCount = g.Count()
            })
            .OrderByDescending(x => x.LastRefundAt)
            .ToListAsync(cancellationToken);

        var total = grouped.Count;
        var page = grouped
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToList();

        if (page.Count == 0)
            return (Array.Empty<AdminRefundOrderSummaryDto>(), total);

        var orderIds = page.Select(x => x.OrderId).ToList();
        var orders = await _unitOfWork.Repository<Order>()
            .AsQueryable()
            .AsNoTracking()
            .Where(o => orderIds.Contains(o.OrderId))
            .Select(o => new
            {
                o.OrderId,
                o.OrderCode,
                o.FinalAmount,
                CustomerFullName = o.User != null ? o.User.FullName : null,
                CustomerEmail = o.User != null ? o.User.Email : null,
                CustomerPhone = o.User != null ? o.User.Phone : null
            })
            .ToListAsync(cancellationToken);

        var refundsByOrder = (await _unitOfWork.Repository<Refund>()
                .FindAsync(r => orderIds.Contains(r.OrderId)))
            .GroupBy(r => r.OrderId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var orderById = orders.ToDictionary(o => o.OrderId);
        var items = page.Select(row =>
        {
            orderById.TryGetValue(row.OrderId, out var order);
            refundsByOrder.TryGetValue(row.OrderId, out var orderRefunds);
            orderRefunds ??= new List<Refund>();

            return new AdminRefundOrderSummaryDto
            {
                OrderId = row.OrderId,
                OrderCode = order?.OrderCode ?? string.Empty,
                CustomerFullName = order?.CustomerFullName,
                CustomerEmail = order?.CustomerEmail,
                CustomerPhone = order?.CustomerPhone,
                OrderFinalAmount = order?.FinalAmount ?? 0,
                TotalRefundAmount = row.TotalRefundAmount,
                PendingRefundAmount = row.PendingRefundAmount,
                RefundCount = row.RefundCount,
                PrimaryRefundStatus = RefundDtoEnricher.ResolvePrimaryRefundStatus(orderRefunds),
                LastRefundAt = row.LastRefundAt
            };
        }).ToList();

        return (items, total);
    }

    public async Task<AdminRefundOrderDetailDto?> GetAdminOrderRefundDetailAsync(
        Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken);
        if (order == null)
            return null;

        var refunds = (await _unitOfWork.Repository<Refund>()
                .FindAsync(r => r.OrderId == orderId))
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        if (refunds.Count == 0)
            return null;

        var activeRefunds = refunds.Where(r => r.Status != RefundState.Rejected).ToList();
        var user = await _unitOfWork.Repository<User>()
            .AsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == order.UserId, cancellationToken);

        var refundDtos = new List<RefundResponseDto>();
        foreach (var refund in refunds)
        {
            var dto = _mapper.Map<RefundResponseDto>(refund);
            RefundDtoEnricher.EnrichRefundResponse(dto, refund, order);
            dto.OrderCode = order.OrderCode;
            dto.CustomerFullName = user?.FullName;
            dto.CustomerEmail = user?.Email;
            dto.CustomerPhone = user?.Phone;
            refundDtos.Add(dto);
        }

        return new AdminRefundOrderDetailDto
        {
            OrderId = order.OrderId,
            OrderCode = order.OrderCode,
            OrderStatus = order.Status.ToString(),
            CustomerFullName = user?.FullName,
            CustomerEmail = user?.Email,
            CustomerPhone = user?.Phone,
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            DeliveryFee = order.DeliveryFee,
            SystemUsageFeeAmount = order.SystemUsageFeeAmount,
            OrderFinalAmount = order.FinalAmount,
            TotalRefundAmount = activeRefunds.Sum(r => r.Amount),
            PendingRefundAmount = activeRefunds
                .Where(r => r.Status == RefundState.Pending)
                .Sum(r => r.Amount),
            Items = RefundDtoEnricher.BuildAdminOrderLineItems(order, refunds),
            Refunds = refundDtos
        };
    }

    private async Task EnrichWithCustomerContactAsync(
        IList<RefundResponseDto> dtos,
        CancellationToken cancellationToken)
    {
        if (dtos.Count == 0)
            return;

        var orderIds = dtos.Select(d => d.OrderId).Distinct().ToList();
        var orders = await _unitOfWork.Repository<Order>()
            .AsQueryable()
            .AsNoTracking()
            .Where(o => orderIds.Contains(o.OrderId))
            .Select(o => new
            {
                o.OrderId,
                o.OrderCode,
                CustomerFullName = o.User != null ? o.User.FullName : null,
                CustomerEmail = o.User != null ? o.User.Email : null,
                CustomerPhone = o.User != null ? o.User.Phone : null
            })
            .ToListAsync(cancellationToken);

        var byOrderId = orders.ToDictionary(o => o.OrderId);
        foreach (var dto in dtos)
        {
            if (!byOrderId.TryGetValue(dto.OrderId, out var order))
                continue;

            dto.OrderCode = order.OrderCode;
            dto.CustomerFullName = order.CustomerFullName;
            dto.CustomerEmail = order.CustomerEmail;
            dto.CustomerPhone = order.CustomerPhone;
        }
    }
}
