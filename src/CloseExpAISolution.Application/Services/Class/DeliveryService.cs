using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Mapbox.Interfaces;
using CloseExpAISolution.Application.Services.Fulfillment;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Application.Services.Routing;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

public class DeliveryService : IDeliveryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IR2StorageService _r2Storage;
    private readonly IMapboxService _mapboxService;
    private readonly ILogger<DeliveryService> _logger;
    private readonly IOrderNotificationPublisher _orderNotificationPublisher;

    public DeliveryService(
        IUnitOfWork unitOfWork,
        IR2StorageService r2Storage,
        IMapboxService mapboxService,
        ILogger<DeliveryService> logger,
        IOrderNotificationPublisher orderNotificationPublisher)
    {
        _unitOfWork = unitOfWork;
        _r2Storage = r2Storage;
        _mapboxService = mapboxService;
        _logger = logger;
        _orderNotificationPublisher = orderNotificationPublisher;
    }

    public async Task<IEnumerable<DeliveryGroupSummaryDto>> GetAvailableDeliveryGroupsAsync(
        Guid deliveryStaffId,
        DateTime? deliveryDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting delivery groups awaiting accept for staff {StaffId}, date: {Date}",
            deliveryStaffId,
            deliveryDate);

        var groups = await _unitOfWork.Repository<DeliveryGroup>()
            .FindAsync(g => g.DeliveryStaffId == deliveryStaffId && g.Status == DeliveryGroupState.Pending);

        if (deliveryDate.HasValue)
        {
            var targetDate = deliveryDate.Value.Date;
            groups = groups.Where(g => g.DeliveryDate.Date == targetDate);
        }

        var result = new List<DeliveryGroupSummaryDto>();
        foreach (var group in groups.OrderBy(g => g.DeliveryDate).ThenBy(g => g.CreatedAt))
        {
            var timeSlot = await _unitOfWork.Repository<DeliveryTimeSlot>()
                .FirstOrDefaultAsync(ts => ts.DeliveryTimeSlotId == group.TimeSlotId);

            var (totalOrders, completedCount) = await GetGroupOrderProgressCountsAsync(group.DeliveryGroupId);

            result.Add(new DeliveryGroupSummaryDto
            {
                DeliveryGroupId = group.DeliveryGroupId,
                GroupCode = group.GroupCode,
                TimeSlotDisplay = timeSlot != null
                    ? $"{timeSlot.StartTime:hh\\:mm} - {timeSlot.EndTime:hh\\:mm}"
                    : "N/A",
                DeliveryType = group.DeliveryType,
                DeliveryArea = group.DeliveryArea,
                CenterLatitude = group.CenterLatitude,
                CenterLongitude = group.CenterLongitude,
                Status = group.Status.ToString(),
                TotalOrders = totalOrders,
                CompletedOrders = completedCount,
                DeliveryDate = group.DeliveryDate
            });
        }

        return result;
    }

    public async Task<(IEnumerable<DeliveryGroupSummaryDto> Items, int TotalCount)> GetMyDeliveryGroupsAsync(
        Guid deliveryStaffId,
        string? status = null,
        DateTime? deliveryDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting delivery groups for staff {StaffId}", deliveryStaffId);

        var allGroups = await _unitOfWork.Repository<DeliveryGroup>()
            .FindAsync(g => g.DeliveryStaffId == deliveryStaffId);

        var filtered = allGroups.AsEnumerable();

        if (!string.IsNullOrEmpty(status))
        {
            filtered = filtered.Where(g => g.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (deliveryDate.HasValue)
        {
            var targetDate = deliveryDate.Value.Date;
            filtered = filtered.Where(g => g.DeliveryDate.Date == targetDate);
        }

        var orderedGroups = filtered
            .OrderByDescending(g => g.DeliveryDate)
            .ThenBy(g => g.CreatedAt)
            .ToList();

        var totalCount = orderedGroups.Count;
        var pagedGroups = orderedGroups
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var result = new List<DeliveryGroupSummaryDto>();
        foreach (var group in pagedGroups)
        {
            var timeSlot = await _unitOfWork.Repository<DeliveryTimeSlot>()
                .FirstOrDefaultAsync(ts => ts.DeliveryTimeSlotId == group.TimeSlotId);

            var (totalOrders, completedCount) = await GetGroupOrderProgressCountsAsync(group.DeliveryGroupId);

            result.Add(new DeliveryGroupSummaryDto
            {
                DeliveryGroupId = group.DeliveryGroupId,
                GroupCode = group.GroupCode,
                TimeSlotDisplay = timeSlot != null
                    ? $"{timeSlot.StartTime:hh\\:mm} - {timeSlot.EndTime:hh\\:mm}"
                    : "N/A",
                DeliveryType = group.DeliveryType,
                DeliveryArea = group.DeliveryArea,
                CenterLatitude = group.CenterLatitude,
                CenterLongitude = group.CenterLongitude,
                Status = group.Status.ToString(),
                TotalOrders = totalOrders,
                CompletedOrders = completedCount,
                DeliveryDate = group.DeliveryDate
            });
        }

        return (result, totalCount);
    }

    public async Task<DeliveryGroupResponseDto?> GetDeliveryGroupDetailAsync(
        Guid deliveryGroupId,
        CancellationToken cancellationToken = default)
    {
        var group = await _unitOfWork.Repository<DeliveryGroup>()
            .FirstOrDefaultAsync(g => g.DeliveryGroupId == deliveryGroupId);

        if (group == null)
            return null;

        return await MapToDeliveryGroupResponseAsync(group);
    }

    public async Task<DeliveryGroupResponseDto> AcceptDeliveryGroupAsync(
        Guid deliveryGroupId,
        Guid deliveryStaffId,
        AcceptDeliveryGroupRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Staff {StaffId} accepting delivery group {GroupId}", deliveryStaffId, deliveryGroupId);

        // Validate delivery staff exists and has correct role
        var staff = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.UserId == deliveryStaffId);

        if (staff == null)
            throw new KeyNotFoundException("Không tìm thấy nhân viên giao hàng.");

        if (staff.RoleId != (int)RoleUser.DeliveryStaff)
            throw new UnauthorizedAccessException("Người dùng không có quyền nhận đơn giao hàng.");

        var group = await _unitOfWork.Repository<DeliveryGroup>()
            .FirstOrDefaultAsync(g => g.DeliveryGroupId == deliveryGroupId);

        if (group == null)
            throw new KeyNotFoundException("Không tìm thấy nhóm giao hàng.");

        if (group.Status != DeliveryGroupState.Pending)
            throw new InvalidOperationException("Nhóm giao hàng không ở trạng thái chờ shipper xác nhận nhận.");

        if (group.DeliveryStaffId == null)
            throw new InvalidOperationException("Nhóm giao hàng chưa được admin gán shipper.");

        if (group.DeliveryStaffId != deliveryStaffId)
            throw new UnauthorizedAccessException("Bạn không phải shipper được admin gán cho nhóm này.");

        group.Status = DeliveryGroupState.Assigned;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            var note = request.Notes.Trim();
            group.Notes = string.IsNullOrEmpty(group.Notes)
                ? note
                : $"{group.Notes} | {note}";
        }

        group.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<DeliveryGroup>().Update(group);

        var ordersInGroup = await _unitOfWork.Repository<Order>()
            .FindAsync(o => o.DeliveryGroupId == deliveryGroupId);
        foreach (var o in ordersInGroup)
        {
            await _orderNotificationPublisher.PublishDeliveryStatusChildAsync(
                o.OrderId,
                o.UserId,
                o.OrderCode,
                DeliveryState.PickedUp,
                cancellationToken: cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Delivery group {GroupId} accepted by staff {StaffId}", deliveryGroupId, deliveryStaffId);

        return await MapToDeliveryGroupResponseAsync(group);
    }

    public async Task<DeliveryGroupResponseDto> StartDeliveryAsync(
        Guid deliveryGroupId,
        Guid deliveryStaffId,
        StartDeliveryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Staff {StaffId} starting delivery for group {GroupId}", deliveryStaffId, deliveryGroupId);

        var group = await _unitOfWork.Repository<DeliveryGroup>()
            .FirstOrDefaultAsync(g => g.DeliveryGroupId == deliveryGroupId);

        if (group == null)
            throw new KeyNotFoundException("Không tìm thấy nhóm giao hàng.");

        if (group.DeliveryStaffId != deliveryStaffId)
            throw new UnauthorizedAccessException("Bạn không được phân công nhóm giao hàng này.");

        // Đã đang giao — idempotent (tránh lỗi khi app gọi lại hoặc xác nhận đơn tự start).
        if (group.Status == DeliveryGroupState.InTransit)
            return await MapToDeliveryGroupResponseAsync(group);

        if (group.Status != DeliveryGroupState.Assigned)
            throw new InvalidOperationException("Nhóm giao hàng phải ở trạng thái 'Đã nhận' để bắt đầu giao.");

        // Update group status
        group.Status = DeliveryGroupState.InTransit;
        if (!string.IsNullOrEmpty(request.Notes))
        {
            group.Notes = string.IsNullOrEmpty(group.Notes)
                ? request.Notes
                : $"{group.Notes} | {request.Notes}";
        }
        group.UpdatedAt = DateTime.UtcNow;

        var groupItems = (await _unitOfWork.Repository<OrderItem>()
                .FindAsync(i => i.DeliveryGroupId == deliveryGroupId))
            .ToList();
        var now = DateTime.UtcNow;
        foreach (var oi in groupItems.Where(i =>
                     i.PackagingStatus == PackagingState.Completed
                     && i.DeliveryStatus is DeliveryState.ReadyToShip or null))
        {
            oi.DeliveryStatus = DeliveryState.InTransit;
            _unitOfWork.Repository<OrderItem>().Update(oi);
        }

        foreach (var oid in groupItems.Select(i => i.OrderId).Distinct())
        {
            var o = await _unitOfWork.Repository<Order>().FirstOrDefaultAsync(x => x.OrderId == oid);
            if (o == null)
                continue;
            var oItems = (await _unitOfWork.Repository<OrderItem>().FindAsync(i => i.OrderId == oid)).ToList();
            OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(o, oItems);
            o.UpdatedAt = now;
            _unitOfWork.Repository<Order>().Update(o);

            await _orderNotificationPublisher.PublishDeliveryStatusChildAsync(
                o.OrderId,
                o.UserId,
                o.OrderCode,
                DeliveryState.InTransit,
                cancellationToken: cancellationToken);
        }

        _unitOfWork.Repository<DeliveryGroup>().Update(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Delivery group {GroupId} is now InTransit", deliveryGroupId);

        return await MapToDeliveryGroupResponseAsync(group);
    }

    public async Task<DeliveryOrderResponseDto?> GetOrderDetailForDeliveryAsync(
        Guid orderId,
        Guid deliveryStaffId,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Repository<Order>()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return null;

        var staffGroup = await ResolveStaffGroupForOrderAsync(order, deliveryStaffId, cancellationToken);
        if (staffGroup == null)
            throw new UnauthorizedAccessException("Bạn không được phân công giao đơn hàng này.");

        return await MapToDeliveryOrderResponseAsync(order, staffGroup.DeliveryGroupId);
    }

    public async Task<DeliveryProofUploadResponseDto> UploadDeliveryProofImageAsync(
        Guid orderId,
        Guid deliveryStaffId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Staff {StaffId} uploading proof image for order {OrderId}", deliveryStaffId, orderId);

        var order = await _unitOfWork.Repository<Order>()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        if (await ResolveStaffGroupForOrderAsync(order, deliveryStaffId, cancellationToken) == null)
            throw new UnauthorizedAccessException("Bạn không được phân công giao đơn hàng này.");

        if (order.Status is not (OrderState.ReadyToShip or OrderState.DeliveredWaitConfirm))
            throw new InvalidOperationException("Đơn hàng phải ở trạng thái phù hợp để tải ảnh chứng minh.");

        ValidateDeliveryProofImageContent(fileName, contentType);

        var url = await _r2Storage.UploadDeliveryProofImageAsync(
            fileStream,
            fileName,
            contentType,
            orderId,
            deliveryStaffId,
            cancellationToken);

        return new DeliveryProofUploadResponseDto { ProofImageUrl = url };
    }

    public async Task<DeliveryOrderResponseDto> ConfirmDeliveryAsync(
        Guid orderId,
        Guid deliveryStaffId,
        ConfirmDeliveryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Staff {StaffId} confirming delivery for order {OrderId}", deliveryStaffId, orderId);

        var order = await _unitOfWork.Repository<Order>()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        var staffGroup = await ResolveStaffGroupForOrderAsync(order, deliveryStaffId, cancellationToken);
        if (staffGroup == null)
            throw new UnauthorizedAccessException("Bạn không được phân công giao đơn hàng này.");

        if (order.Status is not (OrderState.ReadyToShip or OrderState.DeliveredWaitConfirm))
            throw new InvalidOperationException("Đơn hàng phải ở trạng thái phù hợp để xác nhận giao hàng.");

        var proofTrimmed = request.ProofImageUrl?.Trim() ?? string.Empty;
        if (!TryValidateAbsoluteHttpUrl(proofTrimmed, out var proofUrl))
            throw new InvalidOperationException("ProofImageUrl phải là URL http hoặc https hợp lệ.");

        if (string.IsNullOrWhiteSpace(request.VerificationCode))
            throw new InvalidOperationException("Mã QR / mã xác nhận là bắt buộc.");
        if (!string.Equals(
                request.VerificationCode.Trim(),
                order.OrderCode.Trim(),
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Mã QR / mã xác nhận không khớp với mã đơn hàng.");

        var orderItems = (await _unitOfWork.Repository<OrderItem>().FindAsync(oi => oi.OrderId == orderId)).ToList();
        var targetIds = request.OrderItemIds is { Count: > 0 }
            ? request.OrderItemIds.ToHashSet()
            : null;

        if (targetIds != null)
        {
            foreach (var id in targetIds)
            {
                if (orderItems.All(i => i.OrderItemId != id))
                    throw new ArgumentException($"OrderItemId {id} không thuộc đơn hàng.");
            }
        }

        var itemsToConfirm = (targetIds == null
                ? orderItems.Where(i => i.DeliveryGroupId == staffGroup.DeliveryGroupId)
                : orderItems.Where(i => targetIds.Contains(i.OrderItemId)))
            .Where(i => i.PackagingStatus == PackagingState.Completed
                        && i.DeliveryStatus is not (DeliveryState.Completed
                            or DeliveryState.Failed
                            or DeliveryState.DeliveredWaitConfirm))
            .ToList();

        if (itemsToConfirm.Count == 0)
            throw new InvalidOperationException("Không có dòng hàng nào hợp lệ để xác nhận giao trong nhóm này.");

        foreach (var item in itemsToConfirm)
        {
            if (item.DeliveryGroupId != staffGroup.DeliveryGroupId)
                throw new InvalidOperationException($"Dòng {item.OrderItemId} không thuộc nhóm giao của bạn.");
            if (item.DeliveryStatus is DeliveryState.Completed or DeliveryState.Failed)
                throw new InvalidOperationException($"Dòng {item.OrderItemId} đã kết thúc giao.");
        }

        var now = DateTime.UtcNow;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var item in itemsToConfirm)
            {
                item.DeliveryStatus = DeliveryState.DeliveredWaitConfirm;
                item.DeliveredAt = now;
                _unitOfWork.Repository<OrderItem>().Update(item);

                var deliveryRecord = new DeliveryLog
                {
                    DeliveryId = Guid.NewGuid(),
                    OrderId = orderId,
                    OrderItemId = item.OrderItemId,
                    UserId = deliveryStaffId,
                    Status = DeliveryState.DeliveredWaitConfirm,
                    DeliveredAt = now,
                    FailedReason = null,
                    ProofImageUrl = proofUrl
                };
                await _unitOfWork.Repository<DeliveryLog>().AddAsync(deliveryRecord);
            }

            OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(order, orderItems);
            OrderFulfillmentAggregator.SyncOrderDeliveryGroupPointer(order, orderItems);
            order.UpdatedAt = now;
            _unitOfWork.Repository<Order>().Update(order);

            await _orderNotificationPublisher.PublishDeliveryStatusChildAsync(
                orderId,
                order.UserId,
                order.OrderCode,
                DeliveryState.DeliveredWaitConfirm,
                cancellationToken: cancellationToken);

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Order {OrderId} delivered successfully by staff {StaffId}", orderId, deliveryStaffId);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return await MapToDeliveryOrderResponseAsync(order, staffGroup.DeliveryGroupId);
    }

    public async Task<DeliveryOrderResponseDto> ReportDeliveryFailureAsync(
        Guid orderId,
        Guid deliveryStaffId,
        ReportDeliveryFailureRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Staff {StaffId} reporting delivery failure for order {OrderId}", deliveryStaffId, orderId);

        var order = await _unitOfWork.Repository<Order>()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        var staffGroup = await ResolveStaffGroupForOrderAsync(order, deliveryStaffId, cancellationToken);
        if (staffGroup == null)
            throw new UnauthorizedAccessException("Bạn không được phân công giao đơn hàng này.");

        if (order.Status is not (OrderState.ReadyToShip or OrderState.DeliveredWaitConfirm))
            throw new InvalidOperationException("Đơn hàng phải ở trạng thái phù hợp để báo lỗi.");

        var orderItems = (await _unitOfWork.Repository<OrderItem>().FindAsync(oi => oi.OrderId == orderId)).ToList();
        var targetIds = request.OrderItemIds is { Count: > 0 }
            ? request.OrderItemIds.ToHashSet()
            : null;

        if (targetIds != null)
        {
            foreach (var id in targetIds)
            {
                if (orderItems.All(i => i.OrderItemId != id))
                    throw new ArgumentException($"OrderItemId {id} không thuộc đơn hàng.");
            }
        }

        var itemsToFail = (targetIds == null
                ? orderItems.Where(i => i.DeliveryGroupId == staffGroup.DeliveryGroupId)
                : orderItems.Where(i => targetIds.Contains(i.OrderItemId)))
            .Where(i => i.PackagingStatus == PackagingState.Completed
                        && i.DeliveryStatus is not (DeliveryState.Completed
                            or DeliveryState.Failed
                            or DeliveryState.DeliveredWaitConfirm))
            .ToList();

        if (itemsToFail.Count == 0)
            throw new InvalidOperationException("Không có dòng hàng nào hợp lệ để báo lỗi giao trong nhóm này.");

        foreach (var item in itemsToFail)
        {
            if (item.DeliveryGroupId != staffGroup.DeliveryGroupId)
                throw new InvalidOperationException($"Dòng {item.OrderItemId} không thuộc nhóm giao của bạn.");
            if (item.DeliveryStatus is DeliveryState.Completed or DeliveryState.Failed)
                throw new InvalidOperationException($"Dòng {item.OrderItemId} đã kết thúc giao.");
        }

        var now = DateTime.UtcNow;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var item in itemsToFail)
            {
                item.DeliveryStatus = DeliveryState.Failed;
                item.DeliveryFailedReason = request.FailureReason;
                _unitOfWork.Repository<OrderItem>().Update(item);

                var deliveryRecord = new DeliveryLog
                {
                    DeliveryId = Guid.NewGuid(),
                    OrderId = orderId,
                    OrderItemId = item.OrderItemId,
                    UserId = deliveryStaffId,
                    Status = DeliveryState.Failed,
                    FailedReason = request.FailureReason,
                    DeliveredAt = null
                };
                await _unitOfWork.Repository<DeliveryLog>().AddAsync(deliveryRecord);
            }

            OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(order, orderItems);
            OrderFulfillmentAggregator.SyncOrderDeliveryGroupPointer(order, orderItems);
            order.UpdatedAt = now;
            _unitOfWork.Repository<Order>().Update(order);

            await _orderNotificationPublisher.PublishDeliveryStatusChildAsync(
                orderId,
                order.UserId,
                order.OrderCode,
                DeliveryState.Failed,
                request.FailureReason,
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Order {OrderId} delivery failed - Reason: {Reason}", orderId, request.FailureReason);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return await MapToDeliveryOrderResponseAsync(order, staffGroup.DeliveryGroupId);
    }

    public async Task<DeliveryOrderResponseDto> ConfirmOrderReceiptByCustomerAsync(
        Guid orderId,
        Guid customerId,
        ConfirmOrderReceiptRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Customer {CustomerId} confirming receipt for order {OrderId}", customerId, orderId);

        var order = await _unitOfWork.Repository<Order>()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        if (order.UserId != customerId)
            throw new UnauthorizedAccessException("Bạn không có quyền xác nhận đơn hàng này.");

        if (order.Status != OrderState.DeliveredWaitConfirm)
            throw new InvalidOperationException("Đơn hàng phải ở trạng thái 'Đã giao chờ xác nhận' để hoàn tất.");

        var orderItems = (await _unitOfWork.Repository<OrderItem>().FindAsync(oi => oi.OrderId == orderId)).ToList();
        if (!orderItems.Any(i => i.DeliveryStatus == DeliveryState.DeliveredWaitConfirm))
            throw new InvalidOperationException("Không có dòng hàng nào đang chờ khách xác nhận.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            foreach (var item in orderItems.Where(i => i.DeliveryStatus == DeliveryState.DeliveredWaitConfirm))
            {
                item.DeliveryStatus = DeliveryState.Completed;
                item.DeliveredAt ??= now;
                _unitOfWork.Repository<OrderItem>().Update(item);
            }

            OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(order, orderItems);
            OrderFulfillmentAggregator.SyncOrderDeliveryGroupPointer(order, orderItems);
            order.UpdatedAt = now;

            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                var note = request.Notes.Trim();
                order.DeliveryNote = string.IsNullOrWhiteSpace(order.DeliveryNote)
                    ? $"Khách xác nhận: {note}"
                    : $"{order.DeliveryNote} | Khách xác nhận: {note}";
            }

            _unitOfWork.Repository<Order>().Update(order);

            var deliveryLogs = (await _unitOfWork.Repository<DeliveryLog>()
                    .FindAsync(l => l.OrderId == orderId && l.Status == DeliveryState.DeliveredWaitConfirm))
                .ToList();
            foreach (var log in deliveryLogs)

                await _orderNotificationPublisher.PublishDeliveryStatusChildAsync(
                    order.OrderId,
                    order.UserId,
                    order.OrderCode,
                    DeliveryState.Completed,
                    cancellationToken: cancellationToken);

            var deliveryGroup = order.DeliveryGroupId.HasValue
                ? await _unitOfWork.Repository<DeliveryGroup>()
                    .FirstOrDefaultAsync(g => g.DeliveryGroupId == order.DeliveryGroupId.Value)
                : null;

            if (deliveryGroup?.DeliveryStaffId.HasValue == true)
            {
                log.Status = DeliveryState.Completed;
                log.DeliveredAt ??= now;
                _unitOfWork.Repository<DeliveryLog>().Update(log);
            }

            var staffIds = orderItems
                .Where(i => i.DeliveryGroupId.HasValue)
                .Select(i => i.DeliveryGroupId!.Value)
                .Distinct()
                .ToList();
            foreach (var gid in staffIds)
            {
                var deliveryGroup = await _unitOfWork.Repository<DeliveryGroup>()
                    .FirstOrDefaultAsync(g => g.DeliveryGroupId == gid);
                if (deliveryGroup?.DeliveryStaffId is not { } sid)
                    continue;

                var staffNotification = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = sid,
                    Title = "Khách đã xác nhận đơn",
                    Content = $"Khách hàng đã xác nhận hoàn tất đơn {order.OrderCode}.",
                    Type = NotificationType.OrderUpdate,
                    IsRead = false,
                    CreatedAt = now
                };

                await _unitOfWork.Repository<Notification>().AddAsync(staffNotification);
            }

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return await MapToDeliveryOrderResponseAsync(order);
    }

    public async Task<(IEnumerable<DeliveryRecordResponseDto> Items, int TotalCount)> GetDeliveryHistoryAsync(
        Guid deliveryStaffId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var records = await _unitOfWork.Repository<DeliveryLog>()
            .FindAsync(dr => dr.UserId == deliveryStaffId);

        var filtered = records.AsEnumerable();

        if (fromDate.HasValue)
            filtered = filtered.Where(r => r.DeliveredAt >= fromDate.Value);

        if (toDate.HasValue)
            filtered = filtered.Where(r => r.DeliveredAt <= toDate.Value);

        if (!string.IsNullOrEmpty(status))
            filtered = filtered.Where(r =>
                (r.Status ?? DeliveryState.ReadyToShip)
                    .ToString()
                    .Equals(status, StringComparison.OrdinalIgnoreCase));

        var orderedRecords = filtered.OrderByDescending(r => r.DeliveredAt).ToList();
        var totalCount = orderedRecords.Count;
        var pagedRecords = orderedRecords.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        var result = new List<DeliveryRecordResponseDto>();
        foreach (var record in pagedRecords)
        {
            var order = await _unitOfWork.Repository<Order>()
                .FirstOrDefaultAsync(o => o.OrderId == record.OrderId);
            var staff = await _unitOfWork.Repository<User>()
                .FirstOrDefaultAsync(u => u.UserId == record.UserId);

            result.Add(new DeliveryRecordResponseDto
            {
                DeliveryId = record.DeliveryId,
                OrderId = record.OrderId,
                OrderCode = order?.OrderCode ?? "N/A",
                UserId = record.UserId,
                DeliveryStaffName = staff?.FullName ?? "N/A",
                Status = (record.Status ?? DeliveryState.ReadyToShip).ToString(),
                FailureReason = record.FailedReason,
                DeliveredAt = record.DeliveredAt,
                DeliveryLatitude = record.DeliveryLatitude,
                DeliveryLongitude = record.DeliveryLongitude,
                ProofImageUrl = record.ProofImageUrl
            });
        }

        return (result, totalCount);
    }

    public async Task<DeliveryStatsResponseDto> GetDeliveryStatsAsync(
        Guid deliveryStaffId,
        CancellationToken cancellationToken = default)
    {
        var staff = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.UserId == deliveryStaffId);

        if (staff == null)
            throw new KeyNotFoundException("Không tìm thấy nhân viên giao hàng.");

        var groups = await _unitOfWork.Repository<DeliveryGroup>()
            .FindAsync(g => g.DeliveryStaffId == deliveryStaffId);

        var records = await _unitOfWork.Repository<DeliveryLog>()
            .FindAsync(dr => dr.UserId == deliveryStaffId);

        var recordList = records.ToList();
        var completedCount = recordList.Count(r => r.Status == DeliveryState.DeliveredWaitConfirm
                                                  || r.Status == DeliveryState.Completed);
        var failedCount = recordList.Count(r => r.Status == DeliveryState.Failed);
        var totalOrders = recordList.Count;

        var assignedGroups = groups.Where(g => g.Status == DeliveryGroupState.InTransit || g.Status == DeliveryGroupState.Assigned).ToList();
        var inTransitCount = 0;
        foreach (var g in assignedGroups)
        {
            var orders = await _unitOfWork.Repository<Order>()
                .FindAsync(o => o.DeliveryGroupId == g.DeliveryGroupId
                             && o.Status == OrderState.ReadyToShip);
            inTransitCount += orders.Count();
        }

        return new DeliveryStatsResponseDto
        {
            DeliveryStaffId = deliveryStaffId,
            DeliveryStaffName = staff.FullName,
            TotalAssignedGroups = groups.Count(),
            TotalOrders = totalOrders,
            CompletedOrders = completedCount,
            FailedOrders = failedCount,
            PendingOrders = totalOrders - completedCount - failedCount,
            InTransitOrders = inTransitCount,
            CompletionRate = totalOrders > 0
                ? Math.Round((decimal)completedCount / totalOrders * 100, 2)
                : 0,
            LastDeliveryAt = recordList
                .Where(r => r.DeliveredAt.HasValue)
                .OrderByDescending(r => r.DeliveredAt)
                .Select(r => r.DeliveredAt)
                .FirstOrDefault()
        };
    }

    public async Task<DeliveryGroupResponseDto> CompleteDeliveryGroupAsync(
        Guid deliveryGroupId,
        Guid deliveryStaffId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Staff {StaffId} completing delivery group {GroupId}", deliveryStaffId, deliveryGroupId);

        var group = await _unitOfWork.Repository<DeliveryGroup>()
            .FirstOrDefaultAsync(g => g.DeliveryGroupId == deliveryGroupId);

        if (group == null)
            throw new KeyNotFoundException("Không tìm thấy nhóm giao hàng.");

        if (group.DeliveryStaffId != deliveryStaffId)
            throw new UnauthorizedAccessException("Bạn không được phân công nhóm giao hàng này.");

        if (group.Status != DeliveryGroupState.InTransit)
            throw new InvalidOperationException("Nhóm giao hàng phải đang trong quá trình giao để hoàn thành.");

        var groupItems = (await _unitOfWork.Repository<OrderItem>()
                .FindAsync(i => i.DeliveryGroupId == deliveryGroupId))
            .ToList();

        foreach (var orderId in groupItems.Select(i => i.OrderId).Distinct())
        {
            var oItems = (await _unitOfWork.Repository<OrderItem>().FindAsync(i => i.OrderId == orderId)).ToList();
            foreach (var it in oItems.Where(i =>
                         i.DeliveryGroupId == deliveryGroupId
                         && i.PackagingStatus == PackagingState.Completed))
            {
                if (it.DeliveryStatus is not (DeliveryState.Completed or DeliveryState.Failed))
                    throw new InvalidOperationException(
                        "Còn dòng hàng chưa được giao hoặc báo lỗi. Vui lòng xử lý hết trước khi hoàn thành nhóm.");
            }
        }

        group.Status = DeliveryGroupState.Completed;
        group.UpdatedAt = DateTime.UtcNow;

        var now = DateTime.UtcNow;
        foreach (var orderId in groupItems.Select(i => i.OrderId).Distinct())
        {
            var order = await _unitOfWork.Repository<Order>().FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                continue;

            var orderItems = (await _unitOfWork.Repository<OrderItem>().FindAsync(i => i.OrderId == orderId)).ToList();
            OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(order, orderItems);
            OrderFulfillmentAggregator.SyncOrderDeliveryGroupPointer(order, orderItems);
            order.UpdatedAt = now;
            _unitOfWork.Repository<Order>().Update(order);
        }

        _unitOfWork.Repository<DeliveryGroup>().Update(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Delivery group {GroupId} completed", deliveryGroupId);

        return await MapToDeliveryGroupResponseAsync(group);
    }

    public async Task<DeliveryRoutePlanResponseDto> ComputeDeliveryRoutePlanAsync(
        Guid deliveryGroupId,
        Guid deliveryStaffId,
        DeliveryRoutePlanRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var group = await _unitOfWork.Repository<DeliveryGroup>()
            .FirstOrDefaultAsync(g => g.DeliveryGroupId == deliveryGroupId);

        if (group == null)
            throw new KeyNotFoundException("Không tìm thấy nhóm giao hàng.");

        if (group.DeliveryStaffId != deliveryStaffId)
            throw new UnauthorizedAccessException("Bạn không được phân công nhóm giao hàng này.");

        if (group.DeliveryStaffId == null)
            throw new InvalidOperationException("Nhóm giao hàng chưa được gán shipper.");

        var orderIdSet = (await _unitOfWork.Repository<OrderItem>()
                .FindAsync(i => i.DeliveryGroupId == deliveryGroupId))
            .Select(i => i.OrderId)
            .Distinct()
            .ToHashSet();
        foreach (var o in await _unitOfWork.Repository<Order>()
                     .FindAsync(x => x.DeliveryGroupId == deliveryGroupId))
            orderIdSet.Add(o.OrderId);

        var orders = new List<Order>();
        foreach (var oid in orderIdSet)
        {
            var o = await _unitOfWork.Repository<Order>().FirstOrDefaultAsync(x => x.OrderId == oid);
            if (o != null)
                orders.Add(o);
        }

        orders = orders.OrderBy(o => o.OrderCode).ToList();

        var skipped = new List<Guid>();
        var stops = new List<(Guid OrderId, double Lat, double Lng)>();
        foreach (var order in orders)
        {
            if (IsTerminalRouteOrderState(order.Status))
                continue;

            var coord = await GetOrderDeliveryCoordinateAsync(order, cancellationToken);
            if (coord == null)
            {
                skipped.Add(order.OrderId);
                continue;
            }

            stops.Add((order.OrderId, coord.Value.Lat, coord.Value.Lng));
        }

        var metric = NormalizeRouteMetric(request.Metric);

        if (stops.Count == 0)
        {
            return new DeliveryRoutePlanResponseDto
            {
                OrderedOrderIds = Array.Empty<Guid>(),
                TotalDistanceKm = 0,
                TotalDurationMinutes = 0,
                EncodedPolyline = string.Empty,
                PolylineEncoding = "polyline6",
                Metric = metric,
                SkippedOrderIds = skipped
            };
        }

        var totalCoords = 1 + stops.Count;
        if (totalCoords > DeliveryRoutePlanner.MaxCoordinatesPerRequest)
        {
            throw new InvalidOperationException(
                $"Số điểm trên lộ trình ({totalCoords}) vượt giới hạn {DeliveryRoutePlanner.MaxCoordinatesPerRequest}. " +
                "Vui lòng chia nhóm giao hoặc liên hệ quản trị.");
        }

        var (startLat, startLng) = ResolveRouteStart(group, request, stops);

        var matrixCoords = new List<(double Latitude, double Longitude)>(totalCoords)
        {
            (startLat, startLng)
        };
        foreach (var s in stops)
            matrixCoords.Add((s.Lat, s.Lng));

        var matrix = await _mapboxService.GetDrivingMatrixAsync(matrixCoords, cancellationToken);
        if (matrix == null)
            throw new InvalidOperationException(
                "Không lấy được ma trận lộ trình từ Mapbox. Kiểm tra cấu hình token hoặc thử lại sau.");

        var cost = metric == "duration" ? matrix.DurationsSeconds : matrix.DistancesMeters;
        List<int> tourIndices;
        try
        {
            tourIndices = DeliveryRoutePlanner.BuildOptimizedStopOrder(cost, stops.Count);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                "Không tính được thứ tự điểm: " + ex.Message, ex);
        }

        var orderedStops = tourIndices.Select(idx => stops[idx - 1]).ToList();
        var orderedIds = orderedStops.Select(s => s.OrderId).ToList();

        var waypoints = new List<(double Latitude, double Longitude)>
        {
            (startLat, startLng)
        };
        waypoints.AddRange(orderedStops.Select(s => (s.Lat, s.Lng)));
        waypoints = CollapseConsecutiveDuplicateCoordinates(waypoints);

        var route = await _mapboxService.GetDrivingRoutePolylineAsync(waypoints, cancellationToken);
        if (route == null)
            throw new InvalidOperationException(
                "Không lấy được đường đi chi tiết từ Mapbox. Thử lại sau.");

        return new DeliveryRoutePlanResponseDto
        {
            OrderedOrderIds = orderedIds,
            TotalDistanceKm = Math.Round(route.DistanceMeters / 1000d, 3),
            TotalDurationMinutes = Math.Round(route.DurationSeconds / 60d, 1),
            EncodedPolyline = route.EncodedPolyline,
            PolylineEncoding = "polyline6",
            Metric = metric,
            SkippedOrderIds = skipped
        };
    }

    private static bool IsTerminalRouteOrderState(OrderState status) =>
        status is OrderState.Completed
            or OrderState.Failed
            or OrderState.Canceled
            or OrderState.Refunded;

    private static string NormalizeRouteMetric(string? metric)
    {
        if (string.IsNullOrWhiteSpace(metric))
            return "distance";
        var m = metric.Trim().ToLowerInvariant();
        return m == "duration" || m == "time" ? "duration" : "distance";
    }

    private static (double StartLat, double StartLng) ResolveRouteStart(
        DeliveryGroup group,
        DeliveryRoutePlanRequestDto request,
        IReadOnlyList<(Guid OrderId, double Lat, double Lng)> stops)
    {
        if (request.StartLatitude is { } slat && request.StartLongitude is { } slng)
            return (slat, slng);

        if (group.CenterLatitude is { } clat && group.CenterLongitude is { } clng)
            return ((double)clat, (double)clng);

        return (stops[0].Lat, stops[0].Lng);
    }

    private static List<(double Latitude, double Longitude)> CollapseConsecutiveDuplicateCoordinates(
        IReadOnlyList<(double Latitude, double Longitude)> waypoints)
    {
        var result = new List<(double Latitude, double Longitude)>(waypoints.Count);
        foreach (var p in waypoints)
        {
            if (result.Count == 0)
            {
                result.Add(p);
                continue;
            }

            var last = result[^1];
            if (Math.Abs(last.Latitude - p.Latitude) < 1e-7 && Math.Abs(last.Longitude - p.Longitude) < 1e-7)
                continue;
            result.Add(p);
        }

        if (result.Count < 2 && waypoints.Count >= 2)
            return waypoints.Take(2).ToList();

        return result;
    }

    private async Task<(double Lat, double Lng)?> GetOrderDeliveryCoordinateAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        decimal? latitude = null;
        decimal? longitude = null;

        if (order.CollectionId.HasValue)
        {
            var collectionPoint = await _unitOfWork.Repository<CollectionPoint>()
                .FirstOrDefaultAsync(pp => pp.CollectionId == order.CollectionId.Value);
            latitude = collectionPoint?.Latitude;
            longitude = collectionPoint?.Longitude;
        }
        else
        {
            var customerAddress = await _unitOfWork.Repository<CustomerAddress>()
                .FirstOrDefaultAsync(ca => ca.CustomerAddressId == order.AddressId);
            latitude = customerAddress?.Latitude;
            longitude = customerAddress?.Longitude;
        }

        if (latitude == null || longitude == null)
            return null;

        return ((double)latitude.Value, (double)longitude.Value);
    }

    private async Task<DeliveryGroupResponseDto> MapToDeliveryGroupResponseAsync(DeliveryGroup group)
    {
        var timeSlot = await _unitOfWork.Repository<DeliveryTimeSlot>()
            .FirstOrDefaultAsync(ts => ts.DeliveryTimeSlotId == group.TimeSlotId);

        var staff = group.DeliveryStaffId.HasValue
            ? await _unitOfWork.Repository<User>()
                .FirstOrDefaultAsync(u => u.UserId == group.DeliveryStaffId.Value)
            : null;

        var orderIdsFromItems = (await _unitOfWork.Repository<OrderItem>()
                .FindAsync(i => i.DeliveryGroupId == group.DeliveryGroupId))
            .Select(i => i.OrderId)
            .Distinct()
            .ToHashSet();
        var legacyOrders = await _unitOfWork.Repository<Order>()
            .FindAsync(o => o.DeliveryGroupId == group.DeliveryGroupId);
        foreach (var o in legacyOrders)
            orderIdsFromItems.Add(o.OrderId);

        var orders = new List<Order>();
        foreach (var oid in orderIdsFromItems)
        {
            var o = await _unitOfWork.Repository<Order>().FirstOrDefaultAsync(x => x.OrderId == oid);
            if (o != null)
                orders.Add(o);
        }

        var orderDtos = new List<DeliveryOrderResponseDto>();
        foreach (var order in orders.OrderBy(o => o.OrderCode))
        {
            orderDtos.Add(await MapToDeliveryOrderResponseAsync(order, group.DeliveryGroupId));
        }

        var (_, completedCount) = await GetGroupOrderProgressCountsAsync(group.DeliveryGroupId);
        var failedCount = orders.Count(o => o.Status == OrderState.Failed);

        return new DeliveryGroupResponseDto
        {
            DeliveryGroupId = group.DeliveryGroupId,
            GroupCode = group.GroupCode,
            DeliveryStaffId = group.DeliveryStaffId,
            DeliveryStaffName = staff?.FullName,
            TimeSlotId = group.TimeSlotId,
            TimeSlotDisplay = timeSlot != null
                ? $"{timeSlot.StartTime:hh\\:mm} - {timeSlot.EndTime:hh\\:mm}"
                : "N/A",
            DeliveryType = group.DeliveryType,
            DeliveryArea = group.DeliveryArea,
            CenterLatitude = group.CenterLatitude,
            CenterLongitude = group.CenterLongitude,
            Status = group.Status.ToString(),
            TotalOrders = orders.Count,
            CompletedOrders = completedCount,
            FailedOrders = failedCount,
            Notes = group.Notes,
            DeliveryDate = group.DeliveryDate,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt,
            Orders = orderDtos
        };
    }

    private async Task<DeliveryOrderResponseDto> MapToDeliveryOrderResponseAsync(Order order, Guid? scopedDeliveryGroupId = null)
    {
        var customer = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.UserId == order.UserId);

        var timeSlot = await _unitOfWork.Repository<DeliveryTimeSlot>()
            .FirstOrDefaultAsync(ts => ts.DeliveryTimeSlotId == order.TimeSlotId);

        string? collectionPointName = null;
        string? addressLine = null;
        decimal? latitude = null;
        decimal? longitude = null;

        if (order.CollectionId.HasValue)
        {
            var collectionPoint = await _unitOfWork.Repository<CollectionPoint>()
                .FirstOrDefaultAsync(pp => pp.CollectionId == order.CollectionId.Value);
            collectionPointName = collectionPoint?.Name;
            addressLine = collectionPoint?.AddressLine;
            latitude = collectionPoint?.Latitude;
            longitude = collectionPoint?.Longitude;
        }
        else
        {
            var customerAddress = await _unitOfWork.Repository<CustomerAddress>()
                .FirstOrDefaultAsync(ca => ca.CustomerAddressId == order.AddressId);
            collectionPointName = customerAddress?.RecipientName;
            addressLine = customerAddress?.AddressLine;
            latitude = customerAddress?.Latitude;
            longitude = customerAddress?.Longitude;
        }

        var orderItems = await _unitOfWork.Repository<OrderItem>()
            .FindAsync(oi => oi.OrderId == order.OrderId);
        if (scopedDeliveryGroupId.HasValue)
            orderItems = orderItems.Where(oi => oi.DeliveryGroupId == scopedDeliveryGroupId.Value);

        var itemDtos = new List<DeliveryOrderItemDto>();
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

            itemDtos.Add(new DeliveryOrderItemDto
            {
                OrderItemId = item.OrderItemId,
                ProductName = productName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.Quantity * item.UnitPrice,
                PackagingStatus = item.PackagingStatus.ToString(),
                DeliveryStatus = item.DeliveryStatus?.ToString(),
                DeliveryGroupId = item.DeliveryGroupId
            });
        }

        return new DeliveryOrderResponseDto
        {
            OrderId = order.OrderId,
            DeliveryGroupId = order.DeliveryGroupId,
            OrderCode = order.OrderCode,
            Status = order.Status.ToString(),
            DeliveryType = order.DeliveryType,
            TotalAmount = order.TotalAmount,
            DeliveryFee = order.DeliveryFee,
            OrderDate = order.OrderDate,
            CustomerName = customer?.FullName ?? "N/A",
            CustomerPhone = customer?.Phone ?? "N/A",
            CollectionPointName = collectionPointName,
            AddressLine = addressLine,
            Latitude = latitude,
            Longitude = longitude,
            DeliveryNote = order.DeliveryNote,
            TimeSlotDisplay = timeSlot != null
                ? $"{timeSlot.StartTime:hh\\:mm} - {timeSlot.EndTime:hh\\:mm}"
                : "N/A",
            TotalItems = itemDtos.Count,
            Items = itemDtos
        };
    }

    private async Task<(int TotalOrders, int CompletedOrders)> GetGroupOrderProgressCountsAsync(Guid deliveryGroupId)
    {
        var groupItems = (await _unitOfWork.Repository<OrderItem>()
                .FindAsync(i => i.DeliveryGroupId == deliveryGroupId))
            .ToList();

        var orderIds = groupItems.Select(i => i.OrderId).Distinct().ToList();
        if (orderIds.Count == 0)
            return (0, 0);

        var completed = 0;
        foreach (var orderId in orderIds)
        {
            var scopedItems = groupItems
                .Where(i => i.OrderId == orderId && i.PackagingStatus == PackagingState.Completed)
                .ToList();
            if (scopedItems.Count == 0)
                continue;

            var isCompleted = scopedItems.All(i =>
                i.DeliveryStatus is DeliveryState.Completed or DeliveryState.Failed);
            if (isCompleted)
                completed++;
        }

        return (orderIds.Count, completed);
    }
    /// <summary>
    /// Tìm nhóm giao hàng được gán cho <paramref name="deliveryStaffId"/> bao phủ đơn hàng này
    /// (thông qua <see cref="OrderItem.DeliveryGroupId"/> hoặc <see cref="Order.DeliveryGroupId"/> cũ).
    /// </summary>
    private async Task<DeliveryGroup?> ResolveStaffGroupForOrderAsync(
        Order order,
        Guid deliveryStaffId,
        CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.Repository<OrderItem>().FindAsync(oi => oi.OrderId == order.OrderId);
        foreach (var gId in items.Where(i => i.DeliveryGroupId != null).Select(i => i.DeliveryGroupId!.Value).Distinct())
        {
            var g = await _unitOfWork.Repository<DeliveryGroup>()
                .FirstOrDefaultAsync(x => x.DeliveryGroupId == gId);
            if (g != null && g.DeliveryStaffId == deliveryStaffId)
                return g;
        }

        if (order.DeliveryGroupId.HasValue)
        {
            var g = await _unitOfWork.Repository<DeliveryGroup>()
                .FirstOrDefaultAsync(x => x.DeliveryGroupId == order.DeliveryGroupId.Value);
            if (g != null && g.DeliveryStaffId == deliveryStaffId)
                return g;
        }

        return null;
    }

    /// <summary>Chỉ chấp nhận URL tuyệt đối http/https (dùng cho ProofImageUrl khi confirm).</summary>
    private static bool TryValidateAbsoluteHttpUrl(string trimmed, out string normalized)
    {
        normalized = trimmed;
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;
        normalized = uri.ToString();
        return true;
    }

    private static void ValidateDeliveryProofImageContent(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/bmp"
        };
        var allowedExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
        };
        if (!allowedTypes.Contains(contentType) || !allowedExt.Contains(extension))
            throw new InvalidOperationException("Chỉ chấp nhận file ảnh (JPEG, PNG, GIF, WebP, BMP).");
    }
}













