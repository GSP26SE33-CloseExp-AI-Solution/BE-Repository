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

        var pendingOrders = await _unitOfWork.Repository<Order>()
            .FindAsync(o => o.Status == OrderState.Paid);

        var allRecords = await _unitOfWork.Repository<OrderPackaging>()
            .GetAllAsync();

        var recordsByOrder = allRecords
            .GroupBy(r => r.OrderId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.PackagedAt ?? DateTime.MinValue).First());

        var filtered = pendingOrders
            .Where(o => !recordsByOrder.TryGetValue(o.OrderId, out var record)
                     || record.Status != PackagingState.Completed)
            .OrderBy(o => o.OrderDate)
            .ToList();

        var totalCount = filtered.Count;
        var paged = filtered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        var result = new List<PackagingOrderSummaryDto>();
        foreach (var order in paged)
        {
            recordsByOrder.TryGetValue(order.OrderId, out var record);
            result.Add(await MapToSummaryAsync(order, record));
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

        var records = await _unitOfWork.Repository<OrderPackaging>()
            .FindAsync(r => r.OrderId == orderId);
        var latestRecord = records
            .OrderByDescending(r => r.PackagedAt ?? DateTime.MinValue)
            .FirstOrDefault();

        return await MapToDetailAsync(order, latestRecord);
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
            throw new InvalidOperationException("Đơn hàng không ở trạng thái chờ đóng gói.");

        var record = await GetOrCreatePackagingRecordAsync(orderId, packagingStaffId);
        EnsureRecordOwnedByCurrentStaff(record, packagingStaffId);

        if (record.Status == PackagingState.Completed)
            throw new InvalidOperationException("Đơn hàng đã được đóng gói hoàn tất.");

        record.Status = PackagingState.Pending;
        _unitOfWork.Repository<OrderPackaging>().Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Packaging staff {StaffId} confirmed order {OrderId}", packagingStaffId, orderId);

        return await MapToDetailAsync(order, record);
    }

    public async Task<PackagingOrderDetailDto> MarkCollectedAsync(
        Guid orderId,
        Guid packagingStaffId,
        CollectPackagingOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsurePackagingStaffAsync(packagingStaffId);

        var order = await GetOrderForPackagingAsync(orderId);
        var record = await RequirePackagingRecordAsync(orderId);
        EnsureRecordOwnedByCurrentStaff(record, packagingStaffId);

        if (record.Status != PackagingState.Pending)
            throw new InvalidOperationException("Đơn hàng phải được xác nhận trước khi thu gom sản phẩm.");

        record.Status = PackagingState.Packaging;
        _unitOfWork.Repository<OrderPackaging>().Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Packaging staff {StaffId} collected items for order {OrderId}. Notes: {Notes}", packagingStaffId, orderId, request.Notes);

        return await MapToDetailAsync(order, record);
    }

    public async Task<PackagingOrderDetailDto> CompletePackagingAsync(
        Guid orderId,
        Guid packagingStaffId,
        CompletePackagingOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsurePackagingStaffAsync(packagingStaffId);

        var order = await GetOrderForPackagingAsync(orderId);
        var record = await RequirePackagingRecordAsync(orderId);
        EnsureRecordOwnedByCurrentStaff(record, packagingStaffId);

        if (record.Status != PackagingState.Packaging && record.Status != PackagingState.Pending)
            throw new InvalidOperationException("Đơn hàng phải ở trạng thái đã xác nhận hoặc đang thu gom để hoàn tất đóng gói.");

        if (order.Status != OrderState.Paid)
            throw new InvalidOperationException($"Không thể hoàn tất đóng gói vì đơn hàng đang ở trạng thái {order.Status}, không phải Paid.");

        var orderItems = (await _unitOfWork.Repository<OrderItem>().FindAsync(oi => oi.OrderId == orderId)).ToList();
        if (orderItems.Count == 0)
            throw new InvalidOperationException("Đơn hàng không có dòng sản phẩm.");

        var targetIds = request.OrderItemIds is { Count: > 0 }
            ? request.OrderItemIds.ToHashSet()
            : orderItems.Select(i => i.OrderItemId).ToHashSet();

        foreach (var id in targetIds)
        {
            if (orderItems.All(i => i.OrderItemId != id))
                throw new ArgumentException($"OrderItemId {id} không thuộc đơn hàng.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            foreach (var item in orderItems.Where(i => targetIds.Contains(i.OrderItemId)))
            {
                if (item.PackagingStatus == PackagingState.Failed)
                    throw new InvalidOperationException($"Không thể hoàn tất đóng gói cho dòng {item.OrderItemId} đã thất bại.");

                item.PackagingStatus = PackagingState.Completed;
                item.PackagedAt = now;
                OrderFulfillmentAggregator.MarkItemReadyToShip(item);
                _unitOfWork.Repository<OrderItem>().Update(item);
            }

            if (orderItems.All(i => i.PackagingStatus is PackagingState.Completed or PackagingState.Failed))
            {
                record.Status = orderItems.Any(i => i.PackagingStatus == PackagingState.Completed)
                    ? PackagingState.Completed
                    : PackagingState.Failed;
                record.PackagedAt = now;
            }
            else
            {
                if (record.Status == PackagingState.Pending)
                    record.Status = PackagingState.Packaging;
            }

            _unitOfWork.Repository<OrderPackaging>().Update(record);

            var oldOrderStatus = order.Status;
            OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(order, orderItems);
            OrderFulfillmentAggregator.SyncOrderDeliveryGroupPointer(order, orderItems);
            order.UpdatedAt = now;
            _unitOfWork.Repository<Order>().Update(order);

            if (oldOrderStatus != order.Status && order.Status == OrderState.ReadyToShip)
            {
                var customerNotification = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = order.UserId,
                    Title = "Đơn hàng sẵn sàng giao",
                    Content = $"Đơn hàng {order.OrderCode} đã được đóng gói và sẵn sàng giao.",
                    Type = NotificationType.OrderUpdate,
                    IsRead = false,
                    CreatedAt = now
                };
                await _unitOfWork.Repository<Notification>().AddAsync(customerNotification);

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
            }

            await _unitOfWork.CommitTransactionAsync();

            if (oldOrderStatus != order.Status && order.Status == OrderState.ReadyToShip)
            {
                try
                {
                    await TryScheduleDeliveryQrEmailJobAsync(order.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to schedule SendOrderDeliveryQrEmailJob. orderId={OrderId}",
                        order.OrderId);
                }
            }
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        _logger.LogInformation("Packaging staff {StaffId} completed packaging for order {OrderId}. Notes: {Notes}", packagingStaffId, orderId, request.Notes);

        return await MapToDetailAsync(order, record);
    }

    public async Task<PackagingOrderDetailDto> FailPackagingAsync(
        Guid orderId,
        Guid packagingStaffId,
        FailPackagingOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsurePackagingStaffAsync(packagingStaffId);

        var order = await GetOrderForPackagingAsync(orderId);
        var record = await RequirePackagingRecordAsync(orderId);
        EnsureRecordOwnedByCurrentStaff(record, packagingStaffId);

        if (record.Status == PackagingState.Completed)
            throw new InvalidOperationException("Đơn hàng đã đóng gói xong, không thể đánh dấu thất bại.");

        if (record.Status == PackagingState.Failed)
            throw new InvalidOperationException("Đơn hàng đã được ghi nhận đóng gói thất bại trước đó.");

        if (order.Status != OrderState.Paid)
            throw new InvalidOperationException(
                $"Chỉ có thể báo thất bại đóng gói khi đơn đang ở trạng thái Paid. Trạng thái hiện tại: {order.Status}.");

        var failureReason = request.FailureReason.Trim();
        var now = DateTime.UtcNow;
        var oldOrderStatus = order.Status;

        var orderItems = (await _unitOfWork.Repository<OrderItem>().FindAsync(oi => oi.OrderId == orderId)).ToList();
        var partialIds = request.OrderItemIds is { Count: > 0 }
            ? request.OrderItemIds.ToHashSet()
            : null;

        if (partialIds != null)
        {
            foreach (var id in partialIds)
            {
                if (orderItems.All(i => i.OrderItemId != id))
                    throw new ArgumentException($"OrderItemId {id} không thuộc đơn hàng.");
            }
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (partialIds == null)
            {
                await RestoreStockForOrderAsync(orderId, now, cancellationToken);
                await DetachAllOrderItemsFromDeliveryGroupsAsync(order, orderItems, now, cancellationToken);

                foreach (var item in orderItems)
                {
                    item.PackagingStatus = PackagingState.Failed;
                    item.PackagedAt = now;
                    item.DeliveryStatus = null;
                    item.DeliveryGroupId = null;
                    _unitOfWork.Repository<OrderItem>().Update(item);
                }

                record.Status = PackagingState.Failed;
                record.PackagedAt = now;
                _unitOfWork.Repository<OrderPackaging>().Update(record);

                order.Status = OrderState.Failed;
                order.UpdatedAt = now;
                _unitOfWork.Repository<Order>().Update(order);
            }
            else
            {
                var toFail = orderItems.Where(i => partialIds.Contains(i.OrderItemId)).ToList();
                await RestoreStockForOrderItemsAsync(toFail, now, cancellationToken);

                foreach (var item in toFail)
                {
                    var gid = item.DeliveryGroupId;
                    item.PackagingStatus = PackagingState.Failed;
                    item.PackagedAt = now;
                    item.DeliveryStatus = null;
                    item.DeliveryGroupId = null;
                    _unitOfWork.Repository<OrderItem>().Update(item);
                    if (gid.HasValue)
                        await RecalculateDeliveryGroupTotalOrdersAsync(gid.Value, now, cancellationToken);
                }

                if (orderItems.All(i => i.PackagingStatus is PackagingState.Completed or PackagingState.Failed))
                {
                    record.Status = orderItems.Any(i => i.PackagingStatus == PackagingState.Completed)
                        ? PackagingState.Completed
                        : PackagingState.Failed;
                    record.PackagedAt = now;
                }

                _unitOfWork.Repository<OrderPackaging>().Update(record);

                OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(order, orderItems);
                OrderFulfillmentAggregator.SyncOrderDeliveryGroupPointer(order, orderItems);
                order.UpdatedAt = now;
                _unitOfWork.Repository<Order>().Update(order);

                if (!orderItems.All(i => i.PackagingStatus == PackagingState.Failed))
                {
                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogWarning(
                        "Packaging staff {StaffId} marked packaging failed for subset order {OrderId}. Reason: {Reason}",
                        packagingStaffId,
                        orderId,
                        failureReason);
                    return await MapToDetailAsync(order, record);
                }

                record.Status = PackagingState.Failed;
                record.PackagedAt = now;
                _unitOfWork.Repository<OrderPackaging>().Update(record);

                order.Status = OrderState.Failed;
                order.UpdatedAt = now;
                _unitOfWork.Repository<Order>().Update(order);
            }

            var note = $"Đóng gói thất bại: {failureReason}";
            if (!string.IsNullOrWhiteSpace(request.Notes))
                note += $" | Ghi chú: {request.Notes!.Trim()}";

            var statusLog = new OrderStatusLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                FromStatus = oldOrderStatus,
                ToStatus = OrderState.Failed,
                ChangedBy = packagingStaffId.ToString(),
                Note = note.Length > 2000 ? note[..2000] : note,
                ChangedAt = now
            };
            await _unitOfWork.Repository<OrderStatusLog>().AddAsync(statusLog);

            var customerNotification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = order.UserId,
                Title = "Đơn hàng không thể giao — hoàn tiền",
                Content =
                    $"Đơn {order.OrderCode} gặp sự cố khi đóng gói và đã chuyển sang trạng thái thất bại. Yêu cầu hoàn tiền đã được tạo và sẽ được xử lý.",
                Type = NotificationType.OrderUpdate,
                IsRead = false,
                CreatedAt = now
            };
            await _unitOfWork.Repository<Notification>().AddAsync(customerNotification);

            var transactions = (await _unitOfWork.Repository<Transaction>()
                    .FindAsync(t => t.OrderId == orderId && t.PaymentStatus == PaymentState.Paid))
                .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                .ToList();

            var paidTx = transactions.FirstOrDefault();
            if (paidTx == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy giao dịch thanh toán thành công cho đơn hàng, không thể tạo yêu cầu hoàn tiền.");
            }

            var existingRefundTotal = (await _unitOfWork.Repository<Refund>().FindAsync(r =>
                    r.TransactionId == paidTx.TransactionId && r.Status != RefundState.Rejected))
                .Sum(r => r.Amount);

            var refundable = paidTx.Amount - existingRefundTotal;
            if (refundable > 0)
            {
                var refundReason = note.Length > 2000 ? note[..2000] : note;
                await _refundService.CreateAsync(
                    new CreateRefundRequestDto
                    {
                        OrderId = orderId,
                        TransactionId = paidTx.TransactionId,
                        Amount = refundable,
                        Reason = refundReason
                    },
                    cancellationToken);
            }
            else
            {
                _logger.LogWarning(
                    "Packaging fail for order {OrderId}: no refundable amount left on transaction {TxId} (already refunded).",
                    orderId,
                    paidTx.TransactionId);
            }

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        _logger.LogWarning(
            "Packaging staff {StaffId} marked packaging failed for order {OrderId}. Reason: {Reason}",
            packagingStaffId,
            orderId,
            failureReason);

        return await MapToDetailAsync(order, record);
    }

    private async Task RestoreStockForOrderAsync(Guid orderId, DateTime now, CancellationToken cancellationToken)
    {
        var orderItems = (await _unitOfWork.Repository<OrderItem>().FindAsync(oi => oi.OrderId == orderId)).ToList();
        if (orderItems.Count == 0)
            return;

        var requiredByLot = orderItems
            .GroupBy(oi => oi.LotId)
            .Select(g => new { LotId = g.Key, RequiredQuantity = (decimal)g.Sum(x => x.Quantity) })
            .ToList();

        var lotIds = requiredByLot.Select(x => x.LotId).ToList();
        var lots = await _unitOfWork.Repository<StockLot>().FindAsync(l => lotIds.Contains(l.LotId));
        var lotById = lots.ToDictionary(l => l.LotId);

        foreach (var req in requiredByLot)
        {
            if (!lotById.TryGetValue(req.LotId, out var lot))
                throw new InvalidOperationException($"Không tìm thấy StockLot {req.LotId} để hoàn kho cho order {orderId}.");

            lot.Quantity += req.RequiredQuantity;
            lot.UpdatedAt = now;
            _unitOfWork.Repository<StockLot>().Update(lot);
        }
    }

    private async Task DetachAllOrderItemsFromDeliveryGroupsAsync(
        Order order,
        List<OrderItem> orderItems,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var touchedGroups = new HashSet<Guid>();
        foreach (var item in orderItems)
        {
            if (item.DeliveryGroupId.HasValue)
                touchedGroups.Add(item.DeliveryGroupId.Value);
            item.DeliveryGroupId = null;
            _unitOfWork.Repository<OrderItem>().Update(item);
        }

        if (order.DeliveryGroupId.HasValue)
            touchedGroups.Add(order.DeliveryGroupId.Value);

        order.DeliveryGroupId = null;

        foreach (var gid in touchedGroups)
            await RecalculateDeliveryGroupTotalOrdersAsync(gid, now, cancellationToken);
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

    private async Task RecalculateDeliveryGroupTotalOrdersAsync(
        Guid deliveryGroupId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var itemsInGroup = await _unitOfWork.Repository<OrderItem>()
            .FindAsync(i => i.DeliveryGroupId == deliveryGroupId);
        var distinctOrders = itemsInGroup.Select(i => i.OrderId).Distinct().Count();

        var group = await _unitOfWork.Repository<DeliveryGroup>()
            .FirstOrDefaultAsync(g => g.DeliveryGroupId == deliveryGroupId);
        if (group == null)
            return;

        group.TotalOrders = distinctOrders;
        group.UpdatedAt = now;
        _unitOfWork.Repository<DeliveryGroup>().Update(group);
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

    private async Task<OrderPackaging> GetOrCreatePackagingRecordAsync(Guid orderId, Guid packagingStaffId)
    {
        var records = await _unitOfWork.Repository<OrderPackaging>()
            .FindAsync(r => r.OrderId == orderId);

        var existing = records
            .OrderByDescending(r => r.PackagedAt ?? DateTime.MinValue)
            .FirstOrDefault();

        if (existing != null)
            return existing;

        var created = new OrderPackaging
        {
            PackagingId = Guid.NewGuid(),
            OrderId = orderId,
            UserId = packagingStaffId,
            Status = PackagingState.Pending,
            PackagedAt = null
        };

        await _unitOfWork.Repository<OrderPackaging>().AddAsync(created);
        await _unitOfWork.SaveChangesAsync();
        return created;
    }

    private async Task<OrderPackaging> RequirePackagingRecordAsync(Guid orderId)
    {
        var records = await _unitOfWork.Repository<OrderPackaging>()
            .FindAsync(r => r.OrderId == orderId);

        var existing = records
            .OrderByDescending(r => r.PackagedAt ?? DateTime.MinValue)
            .FirstOrDefault();

        if (existing == null)
            throw new InvalidOperationException("Đơn hàng chưa được xác nhận đóng gói.");

        return existing;
    }

    private static void EnsureRecordOwnedByCurrentStaff(OrderPackaging record, Guid packagingStaffId)
    {
        if (record.UserId != packagingStaffId)
            throw new UnauthorizedAccessException("Đơn hàng này đang được xử lý bởi nhân viên đóng gói khác.");
    }

    private async Task<PackagingOrderSummaryDto> MapToSummaryAsync(Order order, OrderPackaging? record)
    {
        var customer = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.UserId == order.UserId);

        var timeSlot = await _unitOfWork.Repository<DeliveryTimeSlot>()
            .FirstOrDefaultAsync(ts => ts.DeliveryTimeSlotId == order.TimeSlotId);

        var orderItems = await _unitOfWork.Repository<OrderItem>()
            .FindAsync(oi => oi.OrderId == order.OrderId);

        return new PackagingOrderSummaryDto
        {
            OrderId = order.OrderId,
            OrderCode = order.OrderCode,
            OrderStatus = order.Status.ToString(),
            PackagingStatus = record?.Status.ToString() ?? "Pending",
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

    private async Task<PackagingOrderDetailDto> MapToDetailAsync(Order order, OrderPackaging? record)
    {
        var summary = await MapToSummaryAsync(order, record);

        var packagingStaff = record != null
            ? await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == record.UserId)
            : null;

        var orderItems = await _unitOfWork.Repository<OrderItem>()
            .FindAsync(oi => oi.OrderId == order.OrderId);

        var itemDtos = new List<PackagingOrderItemDto>();
        foreach (var item in orderItems)
        {
            var lot = await _unitOfWork.Repository<StockLot>()
                .FirstOrDefaultAsync(pl => pl.LotId == item.LotId);

            string productName = "N/A";
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
                DeliveryStatus = item.DeliveryStatus?.ToString()
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
            PackagingRecordId = record?.PackagingId,
            PackagingStaffId = record?.UserId,
            PackagingStaffName = packagingStaff?.FullName,
            PackagedAt = record?.PackagedAt,
            Items = itemDtos
        };
    }
}






