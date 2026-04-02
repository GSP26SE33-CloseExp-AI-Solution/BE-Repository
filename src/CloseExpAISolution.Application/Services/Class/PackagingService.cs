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

public class PackagingService : IPackagingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PackagingService> _logger;
    private readonly IScheduler _scheduler;

    public PackagingService(IUnitOfWork unitOfWork, ILogger<PackagingService> logger, IScheduler scheduler)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _scheduler = scheduler;
    }

    public async Task<(IEnumerable<PackagingOrderSummaryDto> Items, int TotalCount)> GetPendingOrdersAsync(
        Guid packagingStaffId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        await EnsurePackagingStaffAsync(packagingStaffId);

        var pendingOrders = await _unitOfWork.Repository<Order>()
            .FindAsync(o => o.Status == OrderState.PaidProcessing);

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
        if (order.Status != OrderState.PaidProcessing)
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

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            record.Status = PackagingState.Completed;
            record.PackagedAt = DateTime.UtcNow;
            _unitOfWork.Repository<OrderPackaging>().Update(record);

            order.Status = OrderState.ReadyToShip;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Order>().Update(order);

            var customerNotification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = order.UserId,
                Title = "Đơn hàng sẵn sàng giao",
                Content = $"Đơn hàng {order.OrderCode} đã được đóng gói và sẵn sàng giao.",
                Type = NotificationType.OrderUpdate,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
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
                CreatedAt = DateTime.UtcNow
            }).ToList();

            if (notifications.Count > 0)
            {
                await _unitOfWork.Repository<Notification>().AddRangeAsync(notifications);
            }

            await _unitOfWork.CommitTransactionAsync();

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
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        _logger.LogInformation("Packaging staff {StaffId} completed packaging for order {OrderId}. Notes: {Notes}", packagingStaffId, orderId, request.Notes);

        return await MapToDetailAsync(order, record);
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
            await _scheduler.ScheduleJob(jobDetail, trigger);
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
                SubTotal = item.Quantity * item.UnitPrice
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






