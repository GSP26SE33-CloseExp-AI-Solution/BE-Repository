using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Mapbox.Interfaces;
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
    private readonly IMapboxService _mapboxService;
    private readonly ILogger<DeliveryAdminService> _logger;

    public DeliveryAdminService(
        IUnitOfWork unitOfWork,
        IDeliveryService deliveryService,
        IMapboxService mapboxService,
        ILogger<DeliveryAdminService> logger)
    {
        _unitOfWork = unitOfWork;
        _deliveryService = deliveryService;
        _mapboxService = mapboxService;
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

        if (deliveryStaff.Status != UserState.Active)
            throw new InvalidOperationException("Nhân viên giao hàng không ở trạng thái hoạt động.");

        var group = await _unitOfWork.Repository<DeliveryGroup>()
            .FirstOrDefaultAsync(g => g.DeliveryGroupId == deliveryGroupId);

        if (group == null)
            throw new KeyNotFoundException("Không tìm thấy nhóm giao hàng.");

        if (group.Status != DeliveryGroupState.Pending)
            throw new InvalidOperationException("Chỉ có thể gán nhóm giao hàng đang ở trạng thái chờ nhận.");

        if (group.DeliveryStaffId != null)
            throw new InvalidOperationException("Nhóm giao hàng đã được gán nhân viên giao hàng.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            group.DeliveryStaffId = deliveryStaffId;
            group.Status = DeliveryGroupState.Assigned;
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
                Title = "Phân công nhóm giao hàng",
                Content = $"Bạn được phân công nhóm giao hàng {group.GroupCode} cho ngày {group.DeliveryDate:dd/MM/yyyy}.",
                Type = NotificationType.DeliveryUpdate,
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

    public async Task<(IEnumerable<DeliveryGroupSummaryDto> Items, int TotalCount)> GetDraftDeliveryGroupsAsync(
        DraftDeliveryGroupQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var (pageNumber, pageSize) = NormalizePaging(query.PageNumber, query.PageSize);
        var all = await _unitOfWork.Repository<DeliveryGroup>()
            .FindAsync(g => g.Status == DeliveryGroupState.Draft);

        var filtered = all.AsEnumerable();
        if (query.DeliveryDate.HasValue)
            filtered = filtered.Where(g => g.DeliveryDate.Date == query.DeliveryDate.Value.Date);
        if (query.TimeSlotId.HasValue)
            filtered = filtered.Where(g => g.TimeSlotId == query.TimeSlotId.Value);
        if (query.CollectionId.HasValue)
            filtered = filtered.Where(g => g.DeliveryArea == $"COLLECTION:{query.CollectionId.Value}");

        var ordered = filtered.OrderBy(g => g.DeliveryDate).ThenBy(g => g.GroupCode).ToList();
        var total = ordered.Count;
        var paged = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        var summary = new List<DeliveryGroupSummaryDto>(paged.Count);
        foreach (var g in paged)
        {
            var timeSlot = await _unitOfWork.Repository<DeliveryTimeSlot>()
                .FirstOrDefaultAsync(ts => ts.DeliveryTimeSlotId == g.TimeSlotId);
            summary.Add(new DeliveryGroupSummaryDto
            {
                DeliveryGroupId = g.DeliveryGroupId,
                GroupCode = g.GroupCode,
                TimeSlotDisplay = timeSlot != null ? $"{timeSlot.StartTime:hh\\:mm} - {timeSlot.EndTime:hh\\:mm}" : "N/A",
                DeliveryType = g.DeliveryType,
                DeliveryArea = g.DeliveryArea,
                CenterLatitude = g.CenterLatitude,
                CenterLongitude = g.CenterLongitude,
                Status = g.Status.ToString(),
                TotalOrders = g.TotalOrders,
                CompletedOrders = 0,
                DeliveryDate = g.DeliveryDate
            });
        }

        return (summary, total);
    }

    public async Task<IReadOnlyList<DeliveryGroupSummaryDto>> GenerateDraftGroupsAsync(
        GenerateDeliveryGroupDraftRequestDto request,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        if (adminId == Guid.Empty)
            throw new UnauthorizedAccessException("Không thể xác định quản trị viên hiện tại.");

        var admin = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == adminId);
        if (admin == null || admin.RoleId != (int)RoleUser.Admin)
            throw new UnauthorizedAccessException("Người dùng không có quyền điều phối giao hàng.");

        var maxDistanceKm = Math.Clamp(request.MaxDistanceKm, 0.5m, 50m);
        var maxOrdersPerGroup = Math.Clamp(request.MaxOrdersPerGroup, 1, 200);
        var allOrders = await _unitOfWork.Repository<Order>().GetAllAsync();

        var candidates = allOrders
            .Where(o => o.DeliveryGroupId == null)
            .Where(o => o.Status is OrderState.PaidProcessing or OrderState.ReadyToShip or OrderState.DeliveredWaitConfirm)
            .Where(o => !request.DeliveryDate.HasValue || o.OrderDate.Date == request.DeliveryDate.Value.Date)
            .Where(o => !request.TimeSlotId.HasValue || o.TimeSlotId == request.TimeSlotId.Value)
            .Where(o => !request.CollectionId.HasValue || o.CollectionId == request.CollectionId.Value)
            .OrderBy(o => o.OrderDate)
            .ToList();

        var created = new List<DeliveryGroup>();
        var usedOrderIds = new HashSet<Guid>();

        foreach (var anchor in candidates)
        {
            if (usedOrderIds.Contains(anchor.OrderId))
                continue;

            var anchorPoint = await ResolveOrderPointAsync(anchor);
            if (anchorPoint == null)
                continue;

            var keyDate = anchor.OrderDate.Date;
            var keySlot = anchor.TimeSlotId;
            var keyCollection = anchor.CollectionId;

            var bucket = new List<Order> { anchor };
            usedOrderIds.Add(anchor.OrderId);

            foreach (var o in candidates)
            {
                if (usedOrderIds.Contains(o.OrderId))
                    continue;
                if (o.TimeSlotId != keySlot || o.OrderDate.Date != keyDate || o.CollectionId != keyCollection)
                    continue;
                if (bucket.Count >= maxOrdersPerGroup)
                    break;

                var p = await ResolveOrderPointAsync(o);
                if (p == null)
                    continue;

                var distance = await _mapboxService.GetDrivingDistanceKmAsync(anchorPoint.Value.Lat, anchorPoint.Value.Lng, p.Value.Lat, p.Value.Lng, cancellationToken)
                               ?? CalculateHaversineKm(anchorPoint.Value.Lat, anchorPoint.Value.Lng, p.Value.Lat, p.Value.Lng);
                if ((decimal)distance <= maxDistanceKm)
                {
                    bucket.Add(o);
                    usedOrderIds.Add(o.OrderId);
                }
            }

            var pointList = new List<(double Lat, double Lng)>(bucket.Count);
            foreach (var order in bucket)
            {
                var p = await ResolveOrderPointAsync(order);
                if (p.HasValue)
                    pointList.Add(p.Value);
            }

            var centerLat = pointList.Count > 0 ? pointList.Average(p => p.Lat) : anchorPoint.Value.Lat;
            var centerLng = pointList.Count > 0 ? pointList.Average(p => p.Lng) : anchorPoint.Value.Lng;

            var group = new DeliveryGroup
            {
                DeliveryGroupId = Guid.NewGuid(),
                GroupCode = "DRAFT-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
                TimeSlotId = keySlot,
                DeliveryType = keyCollection.HasValue ? "Pickup" : "Delivery",
                DeliveryArea = keyCollection.HasValue ? $"COLLECTION:{keyCollection.Value}" : "DELIVERY",
                CenterLatitude = (decimal)centerLat,
                CenterLongitude = (decimal)centerLng,
                Status = DeliveryGroupState.Draft,
                TotalOrders = bucket.Count,
                Notes = $"Auto draft by admin {adminId}",
                DeliveryDate = keyDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<DeliveryGroup>().AddAsync(group);
            foreach (var o in bucket)
            {
                o.DeliveryGroupId = group.DeliveryGroupId;
                o.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Order>().Update(o);
            }
            created.Add(group);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return created.Select(g => new DeliveryGroupSummaryDto
        {
            DeliveryGroupId = g.DeliveryGroupId,
            GroupCode = g.GroupCode,
            TimeSlotDisplay = string.Empty,
            DeliveryType = g.DeliveryType,
            DeliveryArea = g.DeliveryArea,
            CenterLatitude = g.CenterLatitude,
            CenterLongitude = g.CenterLongitude,
            Status = g.Status.ToString(),
            TotalOrders = g.TotalOrders,
            CompletedOrders = 0,
            DeliveryDate = g.DeliveryDate
        }).ToList();
    }

    public async Task<DeliveryGroupResponseDto> ConfirmDraftGroupAsync(
        Guid deliveryGroupId,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        if (adminId == Guid.Empty)
            throw new UnauthorizedAccessException("Không thể xác định quản trị viên hiện tại.");

        var admin = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == adminId);
        if (admin == null || admin.RoleId != (int)RoleUser.Admin)
            throw new UnauthorizedAccessException("Người dùng không có quyền điều phối giao hàng.");

        var group = await _unitOfWork.Repository<DeliveryGroup>()
            .FirstOrDefaultAsync(g => g.DeliveryGroupId == deliveryGroupId);
        if (group == null)
            throw new KeyNotFoundException("Không tìm thấy nhóm giao hàng.");
        if (group.Status != DeliveryGroupState.Draft)
            throw new InvalidOperationException("Chỉ có thể xác nhận nhóm ở trạng thái Draft.");

        group.Status = DeliveryGroupState.Pending;
        group.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<DeliveryGroup>().Update(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = await _deliveryService.GetDeliveryGroupDetailAsync(deliveryGroupId, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Không thể tải lại nhóm giao hàng sau khi xác nhận.");
        return result;
    }

    private async Task<(double Lat, double Lng)?> ResolveOrderPointAsync(Order order)
    {
        if (order.CollectionId.HasValue)
        {
            var cp = await _unitOfWork.Repository<CollectionPoint>()
                .FirstOrDefaultAsync(x => x.CollectionId == order.CollectionId.Value);
            if (cp?.Latitude.HasValue == true && cp.Longitude.HasValue)
                return ((double)cp.Latitude.Value, (double)cp.Longitude.Value);
            return null;
        }

        if (order.AddressId.HasValue)
        {
            var addr = await _unitOfWork.Repository<CustomerAddress>()
                .FirstOrDefaultAsync(x => x.CustomerAddressId == order.AddressId.Value);
            if (addr != null)
                return ((double)addr.Latitude, (double)addr.Longitude);
        }

        return null;
    }

    private static (int PageNumber, int PageSize) NormalizePaging(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;
        return (pageNumber, pageSize);
    }

    private static double CalculateHaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371d;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    private static double ToRad(double degree) => degree * Math.PI / 180d;
}


