using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

public class DeliveryService : IDeliveryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IR2StorageService _r2Storage;
    private readonly ILogger<DeliveryService> _logger;

    public DeliveryService(
        IUnitOfWork unitOfWork,
        IR2StorageService r2Storage,
        ILogger<DeliveryService> logger)
    {
        _unitOfWork = unitOfWork;
        _r2Storage = r2Storage;
        _logger = logger;
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

            var orders = await _unitOfWork.Repository<Order>()
                .FindAsync(o => o.DeliveryGroupId == group.DeliveryGroupId);
            var completedCount = orders.Count(o => o.Status == OrderState.Completed);

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
                TotalOrders = group.TotalOrders,
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

            var orders = await _unitOfWork.Repository<Order>()
                .FindAsync(o => o.DeliveryGroupId == group.DeliveryGroupId);
            var completedCount = orders.Count(o => o.Status == OrderState.Completed);

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
                TotalOrders = group.TotalOrders,
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

        var orders = await _unitOfWork.Repository<Order>()
            .FindAsync(o => o.DeliveryGroupId == deliveryGroupId
                         && o.Status == OrderState.ReadyToShip);

        foreach (var order in orders)
        {
            order.Status = OrderState.ReadyToShip;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Order>().Update(order);
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

        if (order.DeliveryGroupId.HasValue)
        {
            var group = await _unitOfWork.Repository<DeliveryGroup>()
                .FirstOrDefaultAsync(g => g.DeliveryGroupId == order.DeliveryGroupId.Value);

            if (group != null && group.DeliveryStaffId != deliveryStaffId)
                throw new UnauthorizedAccessException("Bạn không được phân công giao đơn hàng này.");
        }

        return await MapToDeliveryOrderResponseAsync(order);
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

        if (order.DeliveryGroupId.HasValue)
        {
            var group = await _unitOfWork.Repository<DeliveryGroup>()
                .FirstOrDefaultAsync(g => g.DeliveryGroupId == order.DeliveryGroupId.Value);

            if (group != null && group.DeliveryStaffId != deliveryStaffId)
                throw new UnauthorizedAccessException("Bạn không được phân công giao đơn hàng này.");
        }

        if (order.Status != OrderState.ReadyToShip)
            throw new InvalidOperationException("Đơn hàng phải ở trạng thái 'Sẵn sàng giao' để tải ảnh chứng minh.");

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

        // Validate order belongs to a group assigned to this staff
        if (order.DeliveryGroupId.HasValue)
        {
            var group = await _unitOfWork.Repository<DeliveryGroup>()
                .FirstOrDefaultAsync(g => g.DeliveryGroupId == order.DeliveryGroupId.Value);

            if (group != null && group.DeliveryStaffId != deliveryStaffId)
                throw new UnauthorizedAccessException("Bạn không được phân công giao đơn hàng này.");
        }

        if (order.Status != OrderState.ReadyToShip)
            throw new InvalidOperationException("Đơn hàng phải ở trạng thái 'Sẵn sàng giao' để xác nhận giao hàng.");

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

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            order.Status = OrderState.DeliveredWaitConfirm;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Order>().Update(order);

            var deliveryRecord = new DeliveryLog
            {
                DeliveryId = Guid.NewGuid(),
                OrderId = orderId,
                UserId = deliveryStaffId,
                Status = DeliveryState.DeliveredWaitConfirm,
                DeliveredAt = DateTime.UtcNow,
                FailedReason = null
            };
            await _unitOfWork.Repository<DeliveryLog>().AddAsync(deliveryRecord);

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = order.UserId,
                Title = "Cập nhật giao hàng",
                Content = $"Đơn hàng {order.OrderCode} đã được giao. Vui lòng xác nhận nhận hàng.",
                Type = NotificationType.DeliveryUpdate,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<Notification>().AddAsync(notification);

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Order {OrderId} delivered successfully by staff {StaffId}", orderId, deliveryStaffId);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return await MapToDeliveryOrderResponseAsync(order);
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

        // Validate order belongs to a group assigned to this staff
        if (order.DeliveryGroupId.HasValue)
        {
            var group = await _unitOfWork.Repository<DeliveryGroup>()
                .FirstOrDefaultAsync(g => g.DeliveryGroupId == order.DeliveryGroupId.Value);

            if (group != null && group.DeliveryStaffId != deliveryStaffId)
                throw new UnauthorizedAccessException("Bạn không được phân công giao đơn hàng này.");
        }

        if (order.Status != OrderState.ReadyToShip)
            throw new InvalidOperationException("Đơn hàng phải ở trạng thái 'Sẵn sàng giao' để báo lỗi.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            order.Status = OrderState.Failed;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Order>().Update(order);

            var deliveryRecord = new DeliveryLog
            {
                DeliveryId = Guid.NewGuid(),
                OrderId = orderId,
                UserId = deliveryStaffId,
                Status = DeliveryState.Failed,
                FailedReason = request.FailureReason,
                DeliveredAt = null
            };
            await _unitOfWork.Repository<DeliveryLog>().AddAsync(deliveryRecord);

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = order.UserId,
                Title = "Giao hàng thất bại",
                Content = $"Đơn hàng {order.OrderCode} giao thất bại. Lý do: {request.FailureReason}",
                Type = NotificationType.DeliveryUpdate,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<Notification>().AddAsync(notification);

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Order {OrderId} delivery failed - Reason: {Reason}", orderId, request.FailureReason);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return await MapToDeliveryOrderResponseAsync(order);
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

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            order.Status = OrderState.Completed;
            order.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                var note = request.Notes.Trim();
                order.DeliveryNote = string.IsNullOrWhiteSpace(order.DeliveryNote)
                    ? $"Khách xác nhận: {note}"
                    : $"{order.DeliveryNote} | Khách xác nhận: {note}";
            }

            _unitOfWork.Repository<Order>().Update(order);

            var deliveryGroup = order.DeliveryGroupId.HasValue
                ? await _unitOfWork.Repository<DeliveryGroup>()
                    .FirstOrDefaultAsync(g => g.DeliveryGroupId == order.DeliveryGroupId.Value)
                : null;

            if (deliveryGroup?.DeliveryStaffId.HasValue == true)
            {
                var staffNotification = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = deliveryGroup.DeliveryStaffId.Value,
                    Title = "Khách đã xác nhận đơn",
                    Content = $"Khách hàng đã xác nhận hoàn tất đơn {order.OrderCode}.",
                    Type = NotificationType.OrderUpdate,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Notification>().AddAsync(staffNotification);

                var latestDeliveryLog = await _unitOfWork.Repository<DeliveryLog>()
                    .FirstOrDefaultAsync(l => l.OrderId == orderId
                                           && l.UserId == deliveryGroup.DeliveryStaffId.Value
                                           && l.Status == DeliveryState.DeliveredWaitConfirm);

                if (latestDeliveryLog != null)
                {
                    latestDeliveryLog.Status = DeliveryState.Completed;
                    latestDeliveryLog.DeliveredAt ??= DateTime.UtcNow;
                    _unitOfWork.Repository<DeliveryLog>().Update(latestDeliveryLog);
                }
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

        var orders = await _unitOfWork.Repository<Order>()
            .FindAsync(o => o.DeliveryGroupId == deliveryGroupId);

        var pendingOrders = orders.Where(o =>
            o.Status != OrderState.DeliveredWaitConfirm
            && o.Status != OrderState.Completed
            && o.Status != OrderState.Failed)
            .ToList();

        if (pendingOrders.Any())
            throw new InvalidOperationException(
                $"Còn {pendingOrders.Count} đơn hàng chưa được xử lý. Vui lòng giao hoặc báo lỗi tất cả đơn hàng trước khi hoàn thành nhóm.");

        group.Status = DeliveryGroupState.Completed;
        group.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<DeliveryGroup>().Update(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Delivery group {GroupId} completed", deliveryGroupId);

        return await MapToDeliveryGroupResponseAsync(group);
    }

    private async Task<DeliveryGroupResponseDto> MapToDeliveryGroupResponseAsync(DeliveryGroup group)
    {
        var timeSlot = await _unitOfWork.Repository<DeliveryTimeSlot>()
            .FirstOrDefaultAsync(ts => ts.DeliveryTimeSlotId == group.TimeSlotId);

        var staff = group.DeliveryStaffId.HasValue
            ? await _unitOfWork.Repository<User>()
                .FirstOrDefaultAsync(u => u.UserId == group.DeliveryStaffId.Value)
            : null;

        var orders = await _unitOfWork.Repository<Order>()
            .FindAsync(o => o.DeliveryGroupId == group.DeliveryGroupId);

        var orderDtos = new List<DeliveryOrderResponseDto>();
        foreach (var order in orders.OrderBy(o => o.OrderCode))
        {
            orderDtos.Add(await MapToDeliveryOrderResponseAsync(order));
        }

        var completedCount = orders.Count(o =>
            o.Status == OrderState.Completed
            || o.Status == OrderState.DeliveredWaitConfirm);
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
            TotalOrders = group.TotalOrders,
            CompletedOrders = completedCount,
            FailedOrders = failedCount,
            Notes = group.Notes,
            DeliveryDate = group.DeliveryDate,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt,
            Orders = orderDtos
        };
    }

    private async Task<DeliveryOrderResponseDto> MapToDeliveryOrderResponseAsync(Order order)
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
                SubTotal = item.Quantity * item.UnitPrice
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













