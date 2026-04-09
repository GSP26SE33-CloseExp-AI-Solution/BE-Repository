using System;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Mapbox.Interfaces;
using CloseExpAISolution.Application.Services.Fulfillment;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Application.Services.Routing;
using CloseExpAISolution.Domain;
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

        if (group.Status is DeliveryGroupState.Assigned or DeliveryGroupState.InTransit or DeliveryGroupState.Completed or DeliveryGroupState.Failed)
            throw new InvalidOperationException(
                $"Không thể điều phối lại: nhóm {group.GroupCode} đã ở trạng thái {group.Status}.");

        if (group.Status is not (DeliveryGroupState.Confirmed or DeliveryGroupState.Pending))
            throw new InvalidOperationException(
                "Chỉ có thể điều phối nhóm ở trạng thái Confirmed hoặc Pending.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            group.DeliveryStaffId = deliveryStaffId;
            group.Status = DeliveryGroupState.Pending;
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
                Content =
                    $"Bạn được phân công nhóm {group.GroupCode} (ngày {group.DeliveryDate:dd/MM/yyyy}). Vui lòng Accept trong app để xác nhận nhận giao.",
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

    public async Task<(IEnumerable<DeliveryGroupSummaryDto> Items, int TotalCount)> GetDeliveryGroupsForAdminAsync(
        DateTime? deliveryDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        List<DeliveryGroup> list;
        if (string.IsNullOrWhiteSpace(status))
        {
            var all = await _unitOfWork.Repository<DeliveryGroup>()
                .FindAsync(g => g.Status != DeliveryGroupState.Draft);
            list = all.ToList();
        }
        else if (!Enum.TryParse<DeliveryGroupState>(status, ignoreCase: true, out var parsed))
        {
            throw new ArgumentException($"Trạng thái không hợp lệ: {status}", nameof(status));
        }
        else if (parsed == DeliveryGroupState.Pending)
        {
            // Pending = đã gán shipper, chờ Accept — không lẫn nhóm Pending không có staff (không tồn tại trong model mới)
            var all = await _unitOfWork.Repository<DeliveryGroup>()
                .FindAsync(g => g.Status == DeliveryGroupState.Pending && g.DeliveryStaffId != null);
            list = all.ToList();
        }
        else
        {
            var all = await _unitOfWork.Repository<DeliveryGroup>().FindAsync(g => g.Status == parsed);
            list = all.ToList();
        }

        var filtered = list.AsEnumerable();
        if (deliveryDate.HasValue)
            filtered = filtered.Where(g => g.DeliveryDate.Date == deliveryDate.Value.Date);

        var ordered = filtered.OrderBy(g => g.DeliveryDate).ThenBy(g => g.GroupCode).ToList();
        var totalCount = ordered.Count;
        var paged = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        var summaries = await MapGroupsToSummariesAsync(paged, cancellationToken);
        return (summaries, totalCount);
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
        var maxOrdersPerGroup = Math.Clamp( // Max 200 orders per request
            request.MaxOrdersPerGroup,
            1,
            Math.Min(200, DeliveryRoutePlanner.MaxCoordinatesPerRequest - 1));

        var allOrders = (await _unitOfWork.Repository<Order>().GetAllAsync()).ToDictionary(o => o.OrderId);
        var allItems = (await _unitOfWork.Repository<OrderItem>().GetAllAsync()).ToList();

        var lotIds = allItems.Select(i => i.LotId).Distinct().ToList();
        var lots = (await _unitOfWork.Repository<StockLot>().FindAsync(l => lotIds.Contains(l.LotId))).ToDictionary(l => l.LotId);
        var productIds = lots.Values.Select(l => l.ProductId).Distinct().ToList();
        var products = (await _unitOfWork.Repository<Product>().FindAsync(p => productIds.Contains(p.ProductId)))
            .ToDictionary(p => p.ProductId);

        var eligibleItems = new List<OrderItem>();
        foreach (var oi in allItems)
        {
            if (!allOrders.TryGetValue(oi.OrderId, out var order))
                continue;
            if (oi.PackagingStatus != PackagingState.Completed)
                continue;
            if (oi.DeliveryGroupId.HasValue)
                continue;
            if (oi.DeliveryStatus is DeliveryState.Failed or DeliveryState.Completed)
                continue;

            if (order.Status is not (OrderState.Paid or OrderState.ReadyToShip))
                continue;

            if (request.DeliveryDate.HasValue && order.OrderDate.Date != request.DeliveryDate.Value.Date)
                continue;
            if (request.TimeSlotId.HasValue && order.TimeSlotId != request.TimeSlotId.Value)
                continue;
            if (request.CollectionId.HasValue && order.CollectionId != request.CollectionId.Value)
                continue;

            if (!lots.TryGetValue(oi.LotId, out var lot) || !products.TryGetValue(lot.ProductId, out var product))
                continue;

            eligibleItems.Add(oi);
        }

        var byKey = eligibleItems.GroupBy(oi =>
        {
            var order = allOrders[oi.OrderId];
            var lot = lots[oi.LotId];
            var product = products[lot.ProductId];
            return (
                product.SupermarketId,
                order.OrderDate.Date,
                order.TimeSlotId,
                order.CollectionId,
                order.AddressId,
                order.DeliveryType);
        });

        var created = new List<DeliveryGroup>();
        var usedItemIds = new HashSet<Guid>();

        foreach (var keyGroup in byKey)
        {
            var keyItems = keyGroup.Where(i => !usedItemIds.Contains(i.OrderItemId)).ToList();
            while (keyItems.Count > 0)
            {
                var remaining = keyItems.Where(i => !usedItemIds.Contains(i.OrderItemId)).ToList();
                if (remaining.Count == 0)
                    break;

                var candidateOrderIds = remaining.Select(i => i.OrderId).Distinct()
                    .OrderBy(oid => allOrders[oid].OrderDate)
                    .ToList();

                Order? anchorOrder = null;
                (double Lat, double Lng)? anchorPoint = null;
                foreach (var oid in candidateOrderIds)
                {
                    var o = allOrders[oid];
                    var p = await ResolveOrderPointAsync(o);
                    if (p.HasValue)
                    {
                        anchorOrder = o;
                        anchorPoint = p;
                        break;
                    }
                }

                if (anchorOrder == null || anchorPoint == null)
                {
                    foreach (var it in remaining)
                        usedItemIds.Add(it.OrderItemId);
                    break;
                }

                var bucketOrderIds = new HashSet<Guid> { anchorOrder.OrderId };
                var bucketOrders = new List<Order> { anchorOrder };

                foreach (var oid in candidateOrderIds)
                {
                    if (oid == anchorOrder.OrderId)
                        continue;
                    if (bucketOrders.Count >= maxOrdersPerGroup)
                        break;

                    var o = allOrders[oid];
                    var p = await ResolveOrderPointAsync(o);
                    if (p == null)
                        continue;

                    var distance = await _mapboxService.GetDrivingDistanceKmAsync(
                                      anchorPoint.Value.Lat, anchorPoint.Value.Lng, p.Value.Lat, p.Value.Lng, cancellationToken)
                                   ?? CalculateHaversineKm(anchorPoint.Value.Lat, anchorPoint.Value.Lng, p.Value.Lat, p.Value.Lng);
                    if ((decimal)distance <= maxDistanceKm)
                    {
                        bucketOrders.Add(o);
                        bucketOrderIds.Add(o.OrderId);
                    }
                }

                var smId = keyGroup.Key.SupermarketId;
                var keyDate = keyGroup.Key.Date;
                var keySlot = keyGroup.Key.TimeSlotId;
                var keyCollection = keyGroup.Key.CollectionId;

                var pointList = new List<(double Lat, double Lng)>();
                foreach (var o in bucketOrders)
                {
                    var p = await ResolveOrderPointAsync(o);
                    if (p.HasValue)
                        pointList.Add(p.Value);
                }

                var centerLat = pointList.Count > 0 ? pointList.Average(p => p.Lat) : anchorPoint.Value.Lat;
                var centerLng = pointList.Count > 0 ? pointList.Average(p => p.Lng) : anchorPoint.Value.Lng;

                var group = new DeliveryGroup
                {
                    DeliveryGroupId = Guid.NewGuid(),
                    SupermarketId = smId,
                    GroupCode = "DRAFT-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
                    TimeSlotId = keySlot,
                    DeliveryType = keyCollection.HasValue ? DeliveryMethod.Pickup : DeliveryMethod.Delivery,
                    DeliveryArea = keyCollection.HasValue ? $"COLLECTION:{keyCollection.Value}" : "DELIVERY",
                    CenterLatitude = (decimal)centerLat,
                    CenterLongitude = (decimal)centerLng,
                    Status = DeliveryGroupState.Draft,
                    TotalOrders = bucketOrderIds.Count,
                    Notes = $"Auto draft by admin {adminId} (item buckets)",
                    DeliveryDate = keyDate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<DeliveryGroup>().AddAsync(group);

                var itemsToAssign = remaining.Where(i => bucketOrderIds.Contains(i.OrderId)).ToList();
                foreach (var item in itemsToAssign)
                {
                    item.DeliveryGroupId = group.DeliveryGroupId;
                    OrderFulfillmentAggregator.MarkItemReadyToShip(item);
                    _unitOfWork.Repository<OrderItem>().Update(item);
                    usedItemIds.Add(item.OrderItemId);
                }

                foreach (var oid in bucketOrderIds)
                {
                    var o = allOrders[oid];
                    var oItems = allItems.Where(i => i.OrderId == oid).ToList();
                    OrderFulfillmentAggregator.SyncOrderDeliveryGroupPointer(o, oItems);
                    OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(o, oItems);
                    o.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Repository<Order>().Update(o);
                }

                created.Add(group);
                keyItems = keyItems.Where(i => !usedItemIds.Contains(i.OrderItemId)).ToList();
            }
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

        group.Status = DeliveryGroupState.Confirmed;
        group.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<DeliveryGroup>().Update(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = await _deliveryService.GetDeliveryGroupDetailAsync(deliveryGroupId, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Không thể tải lại nhóm giao hàng sau khi xác nhận.");
        return result;
    }

    public async Task<MoveOrderItemsToDraftGroupResultDto> MoveOrderItemsToDraftGroupAsync(
        MoveOrderItemsToDraftGroupRequestDto request,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        if (adminId == Guid.Empty)
            throw new UnauthorizedAccessException("Không thể xác định quản trị viên hiện tại.");

        var admin = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == adminId);
        if (admin == null || admin.RoleId != (int)RoleUser.Admin)
            throw new UnauthorizedAccessException("Người dùng không có quyền điều phối giao hàng.");

        var itemIds = request.OrderItemIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        if (itemIds.Count == 0)
            throw new ArgumentException("Phải truyền ít nhất một OrderItemId hợp lệ.", nameof(request.OrderItemIds));

        var targetId = request.DeliveryGroupId;
        if (targetId.HasValue && targetId.Value == Guid.Empty)
            throw new ArgumentException("Mã nhóm giao hàng không hợp lệ.", nameof(request.DeliveryGroupId));

        var selectedItems = (await _unitOfWork.Repository<OrderItem>()
                .FindAsync(oi => itemIds.Contains(oi.OrderItemId)))
            .ToList();
        if (selectedItems.Count != itemIds.Count)
        {
            var foundIds = selectedItems.Select(i => i.OrderItemId).ToHashSet();
            var missing = itemIds.Where(id => !foundIds.Contains(id)).ToList();
            throw new KeyNotFoundException($"Không tìm thấy OrderItem: {string.Join(", ", missing)}");
        }

        DeliveryGroup? newGroup = null;
        if (targetId.HasValue)
        {
            newGroup = await _unitOfWork.Repository<DeliveryGroup>()
                .FirstOrDefaultAsync(g => g.DeliveryGroupId == targetId.Value);
            if (newGroup == null)
                throw new KeyNotFoundException("Không tìm thấy nhóm giao hàng.");
            if (newGroup.Status is DeliveryGroupState.Assigned or DeliveryGroupState.InTransit or DeliveryGroupState.Completed or DeliveryGroupState.Failed)
                throw new InvalidOperationException("Không thể chuyển orderItem vào nhóm đã được shipper nhận hoặc đã kết thúc.");
            if (newGroup.Status is not (DeliveryGroupState.Draft or DeliveryGroupState.Confirmed or DeliveryGroupState.Pending))
                throw new InvalidOperationException("Chỉ có thể chuyển vào nhóm Draft/Confirmed/Pending.");
        }

        var affectedOrderIds = selectedItems.Select(i => i.OrderId).Distinct().ToList();
        var affectedOrderIdSet = affectedOrderIds.ToHashSet();
        var ordersById = (await _unitOfWork.Repository<Order>().GetAllAsync())
            .Where(o => affectedOrderIdSet.Contains(o.OrderId))
            .ToDictionary(o => o.OrderId);

        var lotIds = selectedItems.Select(i => i.LotId).Distinct().ToList();
        var lots = (await _unitOfWork.Repository<StockLot>().FindAsync(l => lotIds.Contains(l.LotId)))
            .ToDictionary(l => l.LotId);
        var productIds = lots.Values.Select(l => l.ProductId).Distinct().ToList();
        var products = (await _unitOfWork.Repository<Product>().FindAsync(p => productIds.Contains(p.ProductId)))
            .ToDictionary(p => p.ProductId);

        foreach (var item in selectedItems)
        {
            if (!ordersById.TryGetValue(item.OrderId, out var order))
                throw new InvalidOperationException($"Không tìm thấy đơn {item.OrderId} của dòng {item.OrderItemId}.");

            if (item.DeliveryGroupId.HasValue)
            {
                var currentGroup = await _unitOfWork.Repository<DeliveryGroup>()
                    .FirstOrDefaultAsync(x => x.DeliveryGroupId == item.DeliveryGroupId.Value);
                if (currentGroup != null && currentGroup.Status is DeliveryGroupState.Assigned or DeliveryGroupState.InTransit or DeliveryGroupState.Completed or DeliveryGroupState.Failed)
                    throw new InvalidOperationException(
                        $"Không thể chuyển orderItem {item.OrderItemId}: nhóm hiện tại đã ở trạng thái {currentGroup.Status}.");
            }

            if (newGroup != null)
                ValidateItemCompatibilityForRegroup(item, order, newGroup, lots, products);
        }

        if (targetId.HasValue && selectedItems.All(i => i.DeliveryGroupId == targetId.Value))
        {
            return new MoveOrderItemsToDraftGroupResultDto
            {
                UpdatedItemCount = 0,
                UpdatedOrderCount = 0,
                OrderItemIds = selectedItems.Select(i => i.OrderItemId).ToList(),
                OrderIds = affectedOrderIds,
                DeliveryGroupId = targetId
            };
        }

        var oldGroupIds = selectedItems
            .Where(i => i.DeliveryGroupId.HasValue)
            .Select(i => i.DeliveryGroupId!.Value)
            .Distinct()
            .ToList();

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            foreach (var oi in selectedItems)
            {
                oi.DeliveryGroupId = targetId;
                _unitOfWork.Repository<OrderItem>().Update(oi);
            }

            foreach (var orderId in affectedOrderIds)
            {
                var order = await _unitOfWork.Repository<Order>().FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null)
                    continue;

                var orderItems = (await _unitOfWork.Repository<OrderItem>()
                        .FindAsync(oi => oi.OrderId == orderId))
                    .ToList();
                OrderFulfillmentAggregator.SyncOrderDeliveryGroupPointer(order, orderItems);
                OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(order, orderItems);
                order.UpdatedAt = now;
                _unitOfWork.Repository<Order>().Update(order);
            }

            foreach (var gid in oldGroupIds)
                await RecalculateDeliveryGroupTotalOrdersAsync(gid, now, cancellationToken);
            if (targetId.HasValue)
                await RecalculateDeliveryGroupTotalOrdersAsync(targetId.Value, now, cancellationToken);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        _logger.LogInformation(
            "Admin {AdminId} moved {ItemCount} order-items to group {GroupId}",
            adminId,
            selectedItems.Count,
            targetId);

        return new MoveOrderItemsToDraftGroupResultDto
        {
            UpdatedItemCount = selectedItems.Count,
            UpdatedOrderCount = affectedOrderIds.Count,
            OrderItemIds = selectedItems.Select(i => i.OrderItemId).ToList(),
            OrderIds = affectedOrderIds,
            DeliveryGroupId = targetId
        };
    }

    private static void ValidateItemCompatibilityForRegroup(
        OrderItem item,
        Order order,
        DeliveryGroup targetGroup,
        IReadOnlyDictionary<Guid, StockLot> lots,
        IReadOnlyDictionary<Guid, Product> products)
    {
        if (targetGroup.TimeSlotId != order.TimeSlotId || targetGroup.DeliveryDate.Date != order.OrderDate.Date)
            throw new InvalidOperationException(
                $"Không thể chuyển orderItem {item.OrderItemId}: khác khung giờ hoặc ngày giao.");

        if (!string.Equals(targetGroup.DeliveryType, order.DeliveryType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Không thể chuyển orderItem {item.OrderItemId}: khác phương thức giao hàng.");

        if (order.CollectionId.HasValue)
        {
            var expectedArea = $"COLLECTION:{order.CollectionId.Value}";
            if (!string.Equals(targetGroup.DeliveryArea, expectedArea, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Không thể chuyển orderItem {item.OrderItemId}: khác điểm nhận hàng.");
        }
        else if (!string.Equals(targetGroup.DeliveryArea, "DELIVERY", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể chuyển orderItem {item.OrderItemId}: khác khu vực giao hàng.");
        }

        if (!lots.TryGetValue(item.LotId, out var lot) || !products.TryGetValue(lot.ProductId, out var product))
            throw new InvalidOperationException($"Không thể xác định siêu thị của orderItem {item.OrderItemId}.");
        if (targetGroup.SupermarketId != product.SupermarketId)
            throw new InvalidOperationException(
                $"Không thể chuyển orderItem {item.OrderItemId}: khác siêu thị của nhóm đích.");
    }

    private async Task<List<DeliveryGroupSummaryDto>> MapGroupsToSummariesAsync(
        IReadOnlyList<DeliveryGroup> groups,
        CancellationToken cancellationToken)
    {
        var result = new List<DeliveryGroupSummaryDto>(groups.Count);
        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var timeSlot = await _unitOfWork.Repository<DeliveryTimeSlot>()
                .FirstOrDefaultAsync(ts => ts.DeliveryTimeSlotId == group.TimeSlotId);

            var orderIds = (await _unitOfWork.Repository<OrderItem>()
                    .FindAsync(i => i.DeliveryGroupId == group.DeliveryGroupId))
                .Select(i => i.OrderId)
                .Distinct()
                .ToHashSet();
            foreach (var o in await _unitOfWork.Repository<Order>()
                         .FindAsync(x => x.DeliveryGroupId == group.DeliveryGroupId))
                orderIds.Add(o.OrderId);

            var completedCount = 0;
            foreach (var oid in orderIds)
            {
                var o = await _unitOfWork.Repository<Order>().FirstOrDefaultAsync(x => x.OrderId == oid);
                if (o != null && o.Status == OrderState.Completed)
                    completedCount++;
            }

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


