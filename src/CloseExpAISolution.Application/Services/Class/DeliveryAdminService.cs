using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

public class DeliveryAdminService : IDeliveryAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeliveryService _deliveryService;
    private readonly ILogger<DeliveryAdminService> _logger;

    public DeliveryAdminService(
        IUnitOfWork unitOfWork,
        IDeliveryService deliveryService,
        ILogger<DeliveryAdminService> logger)
    {
        _unitOfWork = unitOfWork;
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public async Task<DeliveryGroupResponseDto> AssignGroupToStaffAsync(
        Guid deliveryGroupId,
        Guid deliveryStaffId,
        Guid adminId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (deliveryGroupId == Guid.Empty)
            throw new ArgumentException("Mã nhóm giao hàng không hợp lệ.", nameof(deliveryGroupId));

        if (deliveryStaffId == Guid.Empty)
            throw new ArgumentException("Mã nhân viên giao hàng không hợp lệ.", nameof(deliveryStaffId));

        if (adminId == Guid.Empty)
            throw new UnauthorizedAccessException("Không thể xác định quản trị viên hiện tại.");

        var admin = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.UserId == adminId);

        if (admin == null)
            throw new KeyNotFoundException("Không tìm thấy quản trị viên.");

        if (admin.RoleId != (int)RoleUser.Admin)
            throw new UnauthorizedAccessException("Người dùng không có quyền điều phối giao hàng.");

        var deliveryStaff = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.UserId == deliveryStaffId);

        if (deliveryStaff == null)
            throw new KeyNotFoundException("Không tìm thấy nhân viên giao hàng.");

        if (deliveryStaff.RoleId != (int)RoleUser.DeliveryStaff)
            throw new InvalidOperationException("Người dùng được chọn không phải nhân viên giao hàng.");

        if (!string.Equals(deliveryStaff.Status, UserState.Active.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Nhân viên giao hàng không ở trạng thái hoạt động.");

        var group = await _unitOfWork.Repository<DeliveryGroup>()
            .FirstOrDefaultAsync(g => g.DeliveryGroupId == deliveryGroupId);

        if (group == null)
            throw new KeyNotFoundException("Không tìm thấy nhóm giao hàng.");

        if (!string.Equals(group.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ có thể gán nhóm giao hàng đang ở trạng thái chờ nhận.");

        if (group.DeliveryStaffId != null)
            throw new InvalidOperationException("Nhóm giao hàng đã được gán nhân viên giao hàng.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            group.DeliveryStaffId = deliveryStaffId;
            group.Status = "Assigned";
            group.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(reason))
            {
                var normalizedReason = reason.Trim();
                group.Notes = string.IsNullOrWhiteSpace(group.Notes)
                    ? $"Admin điều phối: {normalizedReason}"
                    : $"{group.Notes} | Admin điều phối: {normalizedReason}";
            }

            _unitOfWork.Repository<DeliveryGroup>().Update(group);

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = deliveryStaffId,
                Content = $"Bạn được phân công nhóm giao hàng {group.GroupCode} cho ngày {group.DeliveryDate:dd/MM/yyyy}.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Notification>().AddAsync(notification);
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        _logger.LogInformation(
            "Admin {AdminId} assigned delivery group {GroupId} to staff {StaffId}",
            adminId,
            deliveryGroupId,
            deliveryStaffId);

        var updatedGroup = await _deliveryService.GetDeliveryGroupDetailAsync(deliveryGroupId, cancellationToken);
        if (updatedGroup == null)
            throw new InvalidOperationException("Không thể tải lại nhóm giao hàng sau khi gán.");

        return updatedGroup;
    }

    public async Task<(IEnumerable<DeliveryGroupSummaryDto> Items, int TotalCount)> GetPendingDeliveryGroupsAsync(
        DateTime? deliveryDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pendingGroups = await _deliveryService.GetAvailableDeliveryGroupsAsync(deliveryDate, cancellationToken);

        var orderedGroups = pendingGroups
            .OrderBy(g => g.DeliveryDate)
            .ThenBy(g => g.GroupCode)
            .ToList();

        var totalCount = orderedGroups.Count;
        var pagedGroups = orderedGroups
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pagedGroups, totalCount);
    }
}