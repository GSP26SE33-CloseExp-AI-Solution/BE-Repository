using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Email.Jobs;
using CloseExpAISolution.Application.Services.Fulfillment;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CloseExpAISolution.Application.Services.Class;

public class PackagingService : IPackagingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PackagingService> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IRefundService _refundService;

    public PackagingService(
        IUnitOfWork unitOfWork,
        ILogger<PackagingService> logger,
        ISchedulerFactory schedulerFactory,
        IRefundService refundService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _schedulerFactory = schedulerFactory;
        _refundService = refundService;
    }

    public async Task<(IEnumerable<PackagingOrderSummaryDto> Items, int TotalCount)> GetPendingOrdersAsync(
        Guid packagingStaffId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        await EnsurePackagingStaffAsync(packagingStaffId);

        var openItems = await _unitOfWork.Repository<OrderItem>()
            .FindAsync(oi =>
                oi.PackagingStatus != PackagingState.Completed && oi.PackagingStatus != PackagingState.Failed);

        var orderIdsNeedingPackaging = openItems.Select(oi => oi.OrderId).Distinct().ToHashSet();

        var paidOrders = (await _unitOfWork.Repository<Order>()
                .FindAsync(o => o.Status == OrderState.Paid && orderIdsNeedingPackaging.Contains(o.OrderId)))
            .OrderBy(o => o.OrderDate)
            .ToList();

        var totalCount = paidOrders.Count;
        var paged = paidOrders.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        var result = new List<PackagingOrderSummaryDto>();
        foreach (var order in paged)
        {
            result.Add(await MapToSummaryAsync(order, cancellationToken));
        }

        return (result, totalCount);
    }

    public async Task<PackagingOrderDetailDto?> GetOrderDetailAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Repository<Order>()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return null;

        return await MapToDetailAsync(order, cancellationToken);
    }

    public async Task<PackagingOrderDetailDto> ConfirmOrderAsync(
        Guid orderId,
        Guid packagingStaffId,
        ConfirmPackagingOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsurePackagingStaffAsync(packagingStaffId);

        var order = await GetOrderForPackagingAsync(orderId);
        if (order.Status != OrderState.Paid)
            throw new InvalidOperationException("Đơn hàng không ở trạng thái chờ đóng gói (Paid).");

        var targets = await ResolveTargetOrderItemsAsync(orderId, request.OrderItemIds, cancellationToken);
        foreach (var item in targets)
            EnsureItemPackagingNotTerminal(item, "xác nhận");

        await RemoveLegacyOrderPackagingRowsAsync(orderId, cancellationToken);

        foreach (var item in targets)
        {
            var record = await GetOrCreateItemPackagingRecordAsync(orderId, item.OrderItemId, packagingStaffId, cancellationToken);
            EnsureRecordOwnedByCurrentStaff(record, packagingStaffId);
            if (record.Status == PackagingState.Completed || record.Status == PackagingState.Failed)
                throw new InvalidOperationException($"Dòng hàng {item.OrderItemId} đã kết thúc đóng gói.");

            record.Status = PackagingState.Pending;
            record.PackagedAt = null;
            _unitOfWork.Repository<OrderPackaging>().Update(record);

            item.PackagingStatus = PackagingState.Pending;
            _unitOfWork.Repository<OrderItem>().Update(item);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Packaging staff {StaffId} confirmed {Count} line(s) for order {OrderId}", packagingStaffId, targets.Count, orderId);

        return await MapToDetailAsync(order, cancellationToken);
    }

    public async Task<PackagingOrderDetailDto> MarkCollectedAsync(
        Guid orderId,
        Guid packagingStaffId,
        CollectPackagingOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsurePackagingStaffAsync(packagingStaffId);

        var order = await GetOrderForPackagingAsync(orderId);
        if (order.Status != OrderState.Paid)
            throw new InvalidOperationException("Đơn hàng không ở trạng thái chờ đóng gói (Paid).");

        var targets = await ResolveTargetOrderItemsAsync(orderId, request.OrderItemIds, cancellationToken);
        foreach (var item in targets)
        {
            if (item.PackagingStatus != PackagingState.Pending)
                throw new InvalidOperationException(
                    $"Dòng hàng {item.OrderItemId} phải ở trạng thái đã xác nhận (Pending) trước khi thu gom.");

            var record = await RequireItemPackagingRecordAsync(orderId, item.OrderItemId, cancellationToken);
            EnsureRecordOwnedByCurrentStaff(record, packagingStaffId);

            if (record.Status != PackagingState.Pending)
                throw new InvalidOperationException("Bản ghi đóng gói không ở trạng thái chờ thu gom.");

            record.Status = PackagingState.Packaging;
            _unitOfWork.Repository<OrderPackaging>().Update(record);

            item.PackagingStatus = PackagingState.Packaging;
            _unitOfWork.Repository<OrderItem>().Update(item);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Packaging staff {StaffId} collected {Count} line(s) for order {OrderId}. Notes: {Notes}", packagingStaffId, targets.Count, orderId, request.Notes);

        return await MapToDetailAsync(order, cancellationToken);
    }

    public async Task<PackagingOrderDetailDto> CompletePackagingAsync(
        Guid orderId,
        Guid packagingStaffId,
        CompletePackagingOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsurePackagingStaffAsync(packagingStaffId);

        var order = await GetOrderForPackagingAsync(orderId);
        if (order.Status != OrderState.Paid)
            throw new InvalidOperationException($"Chỉ đóng gói khi đơn ở Paid. Hiện tại: {order.Status}.");

        var targets = await ResolveTargetOrderItemsAsync(orderId, request.OrderItemIds, cancellationToken);
        foreach (var item in targets)
        {
            if (item.PackagingStatus != PackagingState.Pending && item.PackagingStatus != PackagingState.Packaging)
                throw new InvalidOperationException(
                    $"Dòng hàng {item.OrderItemId} phải ở trạng thái đã xác nhận hoặc đang thu gom.");

            var record = await RequireItemPackagingRecordAsync(orderId, item.OrderItemId, cancellationToken);
            EnsureRecordOwnedByCurrentStaff(record, packagingStaffId);
        }

        var now = DateTime.UtcNow;
        var oldOrderStatus = order.Status;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var item in targets)
            {
                var record = await RequireItemPackagingRecordAsync(orderId, item.OrderItemId, cancellationToken);
                record.Status = PackagingState.Completed;
                record.PackagedAt = now;
                _unitOfWork.Repository<OrderPackaging>().Update(record);

                item.PackagingStatus = PackagingState.Completed;
                item.PackagedAt = now;
                item.DeliveryStatus = DeliveryState.ReadyToShip;
                item.PackagingFailedReason = null;
                _unitOfWork.Repository<OrderItem>().Update(item);
            }

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        var itemsAfterComplete = await GetOrderItemsAsync(orderId, cancellationToken);
        var allLinesPackagingSucceeded = itemsAfterComplete.Count > 0
            && itemsAfterComplete.All(i => i.PackagingStatus == PackagingState.Completed);
        if (!allLinesPackagingSucceeded)
            await NotifyCustomerPartialPackagingAsync(order, targets.Count, now, cancellationToken);

        await RefreshOrderStatusAfterPackagingAsync(orderId, oldOrderStatus, now, cancellationToken);

        try
        {
            await TryScheduleDeliveryQrEmailJobAsync(order.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule SendOrderDeliveryQrEmailJob. orderId={OrderId}", order.OrderId);
        }

        _logger.LogInformation("Packaging staff {StaffId} completed {Count} line(s) for order {OrderId}. Notes: {Notes}", packagingStaffId, targets.Count, orderId, request.Notes);

        return await MapToDetailAsync(await ReloadOrderAsync(orderId, cancellationToken), cancellationToken);
    }

    public async Task<PackagingOrderDetailDto> FailPackagingAsync(
        Guid orderId,
        Guid packagingStaffId,
        FailPackagingOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsurePackagingStaffAsync(packagingStaffId);

        var order = await GetOrderForPackagingAsync(orderId);
        if (order.Status != OrderState.Paid)
            throw new InvalidOperationException($"Chỉ báo thất bại đóng gói khi đơn ở Paid. Hiện tại: {order.Status}.");

        var failureReason = request.FailureReason.Trim();
        var targets = await ResolveTargetOrderItemsForFailAsync(orderId, request.OrderItemIds, cancellationToken);

        foreach (var item in targets)
        {
            if (item.PackagingStatus == PackagingState.Completed)
                throw new InvalidOperationException($"Không thể báo thất bại cho dòng đã đóng gói xong ({item.OrderItemId}).");

            var record = await RequireItemPackagingRecordAsync(orderId, item.OrderItemId, cancellationToken);
            EnsureRecordOwnedByCurrentStaff(record, packagingStaffId);
        }

        var now = DateTime.UtcNow;
        var oldOrderStatus = order.Status;
        var note = BuildFailureNote(failureReason, request.Notes);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await RestoreStockForOrderItemsAsync(targets, now, cancellationToken);

            foreach (var item in targets)
            {
                await DetachItemFromDeliveryGroupIfNeededAsync(item, now, cancellationToken);

                var record = await RequireItemPackagingRecordAsync(orderId, item.OrderItemId, cancellationToken);
                record.Status = PackagingState.Failed;
                record.PackagedAt = now;
                _unitOfWork.Repository<OrderPackaging>().Update(record);

                item.PackagingStatus = PackagingState.Failed;
                item.PackagingFailedReason = note.Length > 2000 ? note[..2000] : note;
                item.DeliveryStatus = null;
                _unitOfWork.Repository<OrderItem>().Update(item);
            }

            var failedAmount = targets.Sum(i => i.TotalPrice);
            await TryRefundForPackagingFailureAsync(orderId, failedAmount, note, cancellationToken);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        await NotifyCustomerPackagingFailureAsync(order, targets.Count, failureReason, now, cancellationToken);
        await RefreshOrderStatusAfterPackagingAsync(orderId, oldOrderStatus, now, cancellationToken);

        if ((await GetOrderItemsAsync(orderId, cancellationToken)).All(i => i.PackagingStatus == PackagingState.Failed))
        {
            await TryAddOrderFailedStatusLogAsync(orderId, oldOrderStatus, note, packagingStaffId, now, cancellationToken);
        }

        _logger.LogWarning(
            "Packaging staff {StaffId} failed {Count} line(s) for order {OrderId}. Reason: {Reason}",
            packagingStaffId,
            targets.Count,
            orderId,
            failureReason);

        return await MapToDetailAsync(await ReloadOrderAsync(orderId, cancellationToken), cancellationToken);
    }

    private static string BuildFailureNote(string failureReason, string? notes)
    {
        var n = $"Đóng gói thất bại: {failureReason}";
        if (!string.IsNullOrWhiteSpace(notes))
            n += $" | Ghi chú: {notes.Trim()}";
        return n;
    }

    private async Task<List<OrderItem>> ResolveTargetOrderItemsAsync(
        Guid orderId,
        IReadOnlyList<Guid>? orderItemIds,
        CancellationToken cancellationToken)
    {
        var all = await GetOrderItemsAsync(orderId, cancellationToken);
        if (all.Count == 0)
            throw new InvalidOperationException("Đơn hàng không có dòng hàng.");

        if (orderItemIds == null || orderItemIds.Count == 0)
            return all;

        var set = orderItemIds.ToHashSet();
        var picked = all.Where(i => set.Contains(i.OrderItemId)).ToList();
        if (picked.Count != set.Count)
            throw new InvalidOperationException("Có mã OrderItem không thuộc đơn hàng hoặc bị trùng.");

        return picked;
    }

    private async Task<List<OrderItem>> ResolveTargetOrderItemsForFailAsync(
        Guid orderId,
        IReadOnlyList<Guid>? orderItemIds,
        CancellationToken cancellationToken)
    {
        var all = await GetOrderItemsAsync(orderId, cancellationToken);
        if (all.Count == 0)
            throw new InvalidOperationException("Đơn hàng không có dòng hàng.");

        if (orderItemIds == null || orderItemIds.Count == 0)
        {
            return all.Where(i => i.PackagingStatus != PackagingState.Completed).ToList();
        }

        var set = orderItemIds.ToHashSet();
        var picked = all.Where(i => set.Contains(i.OrderItemId)).ToList();
        if (picked.Count != set.Count)
            throw new InvalidOperationException("Có mã OrderItem không thuộc đơn hàng hoặc bị trùng.");

        return picked;
    }

    private async Task<List<OrderItem>> GetOrderItemsAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Repository<OrderItem>().FindAsync(oi => oi.OrderId == orderId)).ToList();
    }

    private async Task<Order> ReloadOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await GetOrderForPackagingAsync(orderId);
    }

    private static void EnsureItemPackagingNotTerminal(OrderItem item, string action)
    {
        if (item.PackagingStatus == PackagingState.Completed || item.PackagingStatus == PackagingState.Failed)
            throw new InvalidOperationException($"Không thể {action} dòng hàng đã hoàn tất hoặc đã thất bại.");
    }

    private async Task RemoveLegacyOrderPackagingRowsAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var legacy = (await _unitOfWork.Repository<OrderPackaging>()
            .FindAsync(r => r.OrderId == orderId && r.OrderItemId == null)).ToList();
        foreach (var row in legacy)
            _unitOfWork.Repository<OrderPackaging>().Delete(row);

        if (legacy.Count > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<OrderPackaging> GetOrCreateItemPackagingRecordAsync(
        Guid orderId,
        Guid orderItemId,
        Guid packagingStaffId,
        CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.Repository<OrderPackaging>()
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.OrderItemId == orderItemId);

        if (existing != null)
            return existing;

        var created = new OrderPackaging
        {
            PackagingId = Guid.NewGuid(),
            OrderId = orderId,
            OrderItemId = orderItemId,
            UserId = packagingStaffId,
            Status = PackagingState.Pending,
            PackagedAt = null
        };

        await _unitOfWork.Repository<OrderPackaging>().AddAsync(created);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<OrderPackaging> RequireItemPackagingRecordAsync(
        Guid orderId,
        Guid orderItemId,
        CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.Repository<OrderPackaging>()
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.OrderItemId == orderItemId);

        if (record == null)
            throw new InvalidOperationException($"Dòng hàng {orderItemId} chưa được xác nhận đóng gói (confirm).");

        return record;
    }

    private static void EnsureRecordOwnedByCurrentStaff(OrderPackaging record, Guid packagingStaffId)
    {
        if (record.UserId != packagingStaffId)
            throw new UnauthorizedAccessException("Dòng hàng này đang được xử lý bởi nhân viên đóng gói khác.");
    }

    private async Task NotifyCustomerPartialPackagingAsync(
        Order order,
        int lineCount,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = order.UserId,
            Title = "Cập nhật đóng gói",
            Content =
                lineCount > 1
                    ? $"Đơn {order.OrderCode}: {lineCount} dòng hàng đã được đóng gói xong và sẵn sàng cho bước giao."
                    : $"Đơn {order.OrderCode}: một dòng hàng đã được đóng gói xong và sẵn sàng cho bước giao.",
            Type = NotificationType.OrderUpdate,
            IsRead = false,
            CreatedAt = now
        };
        await _unitOfWork.Repository<Notification>().AddAsync(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyCustomerPackagingFailureAsync(
        Order order,
        int lineCount,
        string failureReason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = order.UserId,
            Title = "Đóng gói thất bại — một phần đơn hàng",
            Content =
                lineCount > 1
                    ? $"Đơn {order.OrderCode}: {lineCount} dòng hàng không thể đóng gói ({failureReason}). Hoàn tiền tương ứng đã được yêu cầu khi áp dụng."
                    : $"Đơn {order.OrderCode}: một dòng hàng không thể đóng gói ({failureReason}). Hoàn tiền tương ứng đã được yêu cầu khi áp dụng.",
            Type = NotificationType.OrderUpdate,
            IsRead = false,
            CreatedAt = now
        };
        await _unitOfWork.Repository<Notification>().AddAsync(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RefreshOrderStatusAfterPackagingAsync(
        Guid orderId,
        OrderState previousStatus,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Repository<Order>().FirstOrDefaultAsync(o => o.OrderId == orderId)
            ?? throw new InvalidOperationException("Order not found after packaging update.");

        var items = await GetOrderItemsAsync(orderId, cancellationToken);
        if (items.Count == 0)
            return;

        var allTerminal = items.All(i =>
            i.PackagingStatus == PackagingState.Completed || i.PackagingStatus == PackagingState.Failed);

        if (!allTerminal)
            return;

        var allFailed = items.All(i => i.PackagingStatus == PackagingState.Failed);
        var anyCompleted = items.Any(i => i.PackagingStatus == PackagingState.Completed);

        if (allFailed)
        {
            if (order.Status != OrderState.Failed)
            {
                order.Status = OrderState.Failed;
                order.UpdatedAt = now;
                await DetachOrderFromDeliveryGroupIfNeededAsync(order, now, cancellationToken);
                _unitOfWork.Repository<Order>().Update(order);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (anyCompleted && order.Status != OrderState.ReadyToShip)
        {
            order.Status = OrderState.ReadyToShip;
            order.UpdatedAt = now;
            _unitOfWork.Repository<Order>().Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (previousStatus != OrderState.ReadyToShip)
            {
                await NotifyDeliveryStaffOrderReadyAsync(order, now, cancellationToken);
                var anyFailed = items.Any(i => i.PackagingStatus == PackagingState.Failed);
                var title = "Đơn hàng sẵn sàng giao";
                var content = anyFailed
                    ? $"Đơn {order.OrderCode} đã xử lý đóng gói xong: phần thành công sẵn sàng giao; có dòng hàng đã thất bại (xem chi tiết đơn)."
                    : $"Đơn {order.OrderCode} đã hoàn tất đóng gói (tất cả dòng hàng) và sẵn sàng giao.";

                var customer = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = order.UserId,
                    Title = title,
                    Content = content,
                    Type = NotificationType.OrderUpdate,
                    IsRead = false,
                    CreatedAt = now
                };
                await _unitOfWork.Repository<Notification>().AddAsync(customer);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task DetachOrderFromDeliveryGroupIfNeededAsync(Order order, DateTime now, CancellationToken cancellationToken)
    {
        if (!order.DeliveryGroupId.HasValue)
            return;

        var groupId = order.DeliveryGroupId.Value;
        var group = await _unitOfWork.Repository<DeliveryGroup>()
            .FirstOrDefaultAsync(g => g.DeliveryGroupId == groupId);

        if (group != null && group.TotalOrders > 0)
        {
            group.TotalOrders -= 1;
            group.UpdatedAt = now;
            _unitOfWork.Repository<DeliveryGroup>().Update(group);
        }

        order.DeliveryGroupId = null;
    }

    private async Task NotifyDeliveryStaffOrderReadyAsync(Order order, DateTime now, CancellationToken cancellationToken)
    {
        var deliveryStaffs = await _unitOfWork.Repository<User>()
            .FindAsync(u => u.RoleId == (int)RoleUser.DeliveryStaff);

        var notifications = deliveryStaffs.Select(staff => new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = staff.UserId,
            Title = "Có đơn cần giao",
            Content = $"Đơn hàng {order.OrderCode} đã sẵn sàng để giao.",
            Type = NotificationType.DeliveryUpdate,
            IsRead = false,
            CreatedAt = now
        }).ToList();

        if (notifications.Count > 0)
            await _unitOfWork.Repository<Notification>().AddRangeAsync(notifications);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task TryAddOrderFailedStatusLogAsync(
        Guid orderId,
        OrderState fromStatus,
        string note,
        Guid packagingStaffId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var log = new OrderStatusLog
        {
            LogId = Guid.NewGuid(),
            OrderId = orderId,
            FromStatus = fromStatus,
            ToStatus = OrderState.Failed,
            ChangedBy = packagingStaffId.ToString(),
            Note = note.Length > 2000 ? note[..2000] : note,
            ChangedAt = now
        };
        await _unitOfWork.Repository<OrderStatusLog>().AddAsync(log);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task TryRefundForPackagingFailureAsync(
        Guid orderId,
        decimal refundAmount,
        string reason,
        CancellationToken cancellationToken)
    {
        if (refundAmount <= 0)
            return;

        var transactions = (await _unitOfWork.Repository<Transaction>()
                .FindAsync(t => t.OrderId == orderId && t.PaymentStatus == PaymentState.Paid))
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .ToList();

        var paidTx = transactions.FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Không tìm thấy giao dịch thanh toán thành công, không thể tạo yêu cầu hoàn tiền.");

        var existingRefundTotal = (await _unitOfWork.Repository<Refund>().FindAsync(r =>
                r.TransactionId == paidTx.TransactionId && r.Status != RefundState.Rejected))
            .Sum(r => r.Amount);

        var refundable = paidTx.Amount - existingRefundTotal;
        if (refundable <= 0)
        {
            _logger.LogWarning("Order {OrderId}: no refundable balance left on transaction {TxId}.", orderId, paidTx.TransactionId);
            return;
        }

        var amount = Math.Min(refundAmount, refundable);
        if (amount <= 0)
            return;

        var refundReason = reason.Length > 2000 ? reason[..2000] : reason;
        await _refundService.CreateAsync(
            new CreateRefundRequestDto
            {
                OrderId = orderId,
                TransactionId = paidTx.TransactionId,
                Amount = amount,
                Reason = refundReason
            },
            cancellationToken);
    }

    private async Task RestoreStockForOrderItemsAsync(
        IReadOnlyList<OrderItem> items,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return;

        var requiredByLot = items
            .GroupBy(oi => oi.LotId)
            .Select(g => new { LotId = g.Key, RequiredQuantity = (decimal)g.Sum(x => x.Quantity) })
            .ToList();

        var lotIds = requiredByLot.Select(x => x.LotId).ToList();
        var lots = await _unitOfWork.Repository<StockLot>().FindAsync(l => lotIds.Contains(l.LotId));
        var lotById = lots.ToDictionary(l => l.LotId);

        foreach (var req in requiredByLot)
        {
            if (!lotById.TryGetValue(req.LotId, out var lot))
                throw new InvalidOperationException($"Không tìm thấy StockLot {req.LotId} để hoàn kho.");

            lot.Quantity += req.RequiredQuantity;
            lot.UpdatedAt = now;
            _unitOfWork.Repository<StockLot>().Update(lot);
        }
    }

    private async Task DetachItemFromDeliveryGroupIfNeededAsync(OrderItem item, DateTime now, CancellationToken cancellationToken)
    {
        if (!item.DeliveryGroupId.HasValue)
            return;

        var groupId = item.DeliveryGroupId.Value;
        var group = await _unitOfWork.Repository<DeliveryGroup>()
            .FirstOrDefaultAsync(g => g.DeliveryGroupId == deliveryGroupId);
        if (group == null)
            return;

        if (group != null && group.TotalOrders > 0)
        {
            group.TotalOrders -= 1;
            group.UpdatedAt = now;
            _unitOfWork.Repository<DeliveryGroup>().Update(group);
        }

        item.DeliveryGroupId = null;
        _unitOfWork.Repository<OrderItem>().Update(item);
    }

    private async Task TryScheduleDeliveryQrEmailJobAsync(Guid orderId)
    {
        var jobKey = new JobKey($"SendOrderDeliveryQrEmailJob:{orderId}", "delivery-qr-email");
        var triggerKey = new TriggerKey($"SendOrderDeliveryQrEmailJobTrigger:{orderId}", "delivery-qr-email");

        var jobDetail = JobBuilder.Create<SendOrderDeliveryQrEmailJob>()
            .WithIdentity(jobKey)
            .UsingJobData("orderId", orderId.ToString())
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
        catch (Quartz.ObjectAlreadyExistsException)
        {
            _logger.LogInformation("SendOrderDeliveryQrEmailJob already scheduled. orderId={OrderId}", orderId);
        }
    }

    private async Task EnsurePackagingStaffAsync(Guid userId)
    {
        var user = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy nhân viên đóng gói.");

        if (user.RoleId != (int)RoleUser.PackagingStaff)
            throw new UnauthorizedAccessException("Người dùng không có quyền thực hiện đóng gói.");
    }

    private async Task<Order> GetOrderForPackagingAsync(Guid orderId)
    {
        var order = await _unitOfWork.Repository<Order>()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        return order;
    }

    private async Task<PackagingOrderSummaryDto> MapToSummaryAsync(Order order, CancellationToken cancellationToken)
    {
        var customer = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.UserId == order.UserId);

        var timeSlot = await _unitOfWork.Repository<DeliveryTimeSlot>()
            .FirstOrDefaultAsync(ts => ts.DeliveryTimeSlotId == order.TimeSlotId);

        var orderItems = await GetOrderItemsAsync(order.OrderId, cancellationToken);

        return new PackagingOrderSummaryDto
        {
            OrderId = order.OrderId,
            OrderCode = order.OrderCode,
            OrderStatus = order.Status.ToString(),
            PackagingStatus = BuildPackagingProgressSummary(orderItems),
            CustomerName = customer?.FullName ?? "N/A",
            TimeSlotDisplay = timeSlot != null
                ? $"{timeSlot.StartTime:hh\\:mm} - {timeSlot.EndTime:hh\\:mm}"
                : "N/A",
            DeliveryType = order.DeliveryType,
            TotalItems = orderItems.Sum(i => i.Quantity),
            FinalAmount = order.FinalAmount,
            OrderDate = order.OrderDate
        };
    }

    private static string BuildPackagingProgressSummary(IReadOnlyList<OrderItem> items)
    {
        if (items.Count == 0)
            return "—";

        var done = items.Count(i => i.PackagingStatus == PackagingState.Completed);
        var failed = items.Count(i => i.PackagingStatus == PackagingState.Failed);
        var open = items.Count - done - failed;

        if (open > 0)
            return $"{done}/{items.Count} dòng đã đóng gói xong, {open} dòng đang xử lý";

        return failed == 0
            ? "Tất cả dòng đã đóng gói xong"
            : $"{done} thành công, {failed} thất bại";
    }

    private async Task<PackagingOrderDetailDto> MapToDetailAsync(Order order, CancellationToken cancellationToken)
    {
        var summary = await MapToSummaryAsync(order, cancellationToken);

        var orderItems = await GetOrderItemsAsync(order.OrderId, cancellationToken);

        var packagingRows = (await _unitOfWork.Repository<OrderPackaging>()
            .FindAsync(r => r.OrderId == order.OrderId && r.OrderItemId != null)).ToList();

        Guid? staffId = null;
        string? staffName = null;
        var staffIds = packagingRows.Select(r => r.UserId).Distinct().ToList();
        if (staffIds.Count == 1)
        {
            staffId = staffIds[0];
            var u = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(x => x.UserId == staffId);
            staffName = u?.FullName;
        }

        DateTime? lastAt = orderItems.Any(i => i.PackagedAt.HasValue)
            ? orderItems.Where(i => i.PackagedAt.HasValue).Max(i => i.PackagedAt)
            : null;

        var itemDtos = new List<PackagingOrderItemDto>();
        foreach (var item in orderItems)
        {
            var lot = await _unitOfWork.Repository<StockLot>()
                .FirstOrDefaultAsync(pl => pl.LotId == item.LotId);

            var productName = "N/A";
            if (lot != null)
            {
                var product = await _unitOfWork.Repository<Product>()
                    .FirstOrDefaultAsync(p => p.ProductId == lot.ProductId);
                productName = product?.Name ?? "N/A";
            }

            itemDtos.Add(new PackagingOrderItemDto
            {
                OrderItemId = item.OrderItemId,
                ProductName = productName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.Quantity * item.UnitPrice,
                PackagingStatus = item.PackagingStatus.ToString(),
                DeliveryStatus = item.DeliveryStatus?.ToString(),
                PackagedAt = item.PackagedAt,
                PackagingFailedReason = item.PackagingFailedReason
            });
        }

        return new PackagingOrderDetailDto
        {
            OrderId = summary.OrderId,
            OrderCode = summary.OrderCode,
            OrderStatus = summary.OrderStatus,
            PackagingStatus = summary.PackagingStatus,
            CustomerName = summary.CustomerName,
            TimeSlotDisplay = summary.TimeSlotDisplay,
            DeliveryType = summary.DeliveryType,
            TotalItems = summary.TotalItems,
            FinalAmount = summary.FinalAmount,
            OrderDate = summary.OrderDate,
            PackagingStaffId = staffId,
            PackagingStaffName = staffName,
            LastPackagedAt = lastAt,
            Items = itemDtos
        };
    }
}

