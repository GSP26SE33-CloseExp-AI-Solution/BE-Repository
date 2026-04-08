using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class OrderNotificationPublisher : IOrderNotificationPublisher
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderNotificationPublisher(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task PublishOrderPlacedAsync(Guid orderId, Guid userId, string orderCode, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            OrderId = orderId,
            ParentNotificationId = null,
            Title = "Đơn hàng đã đặt",
            Content =
                $"Đơn {orderCode} đã được tạo thành công. Bạn sẽ nhận thông báo tại đây khi trạng thái đơn thay đổi.",
            Type = NotificationType.OrderUpdate,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Notification>().AddAsync(notification);
    }

    public async Task PublishOrderStatusChangedAsync(
        Guid orderId,
        Guid userId,
        string orderCode,
        OrderState newStatus,
        CancellationToken cancellationToken = default)
    {
        var parentId = await ResolveParentNotificationIdAsync(orderId, userId, cancellationToken);
        var label = GetOrderStatusDisplayName(newStatus);
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            OrderId = orderId,
            ParentNotificationId = parentId,
            Title = "Cập nhật trạng thái đơn hàng",
            Content = $"Đơn {orderCode}: {label}.",
            Type = NotificationType.OrderUpdate,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Notification>().AddAsync(notification);
    }

    public async Task PublishOrderThreadChildAsync(
        Guid orderId,
        Guid userId,
        string orderCode,
        string title,
        string content,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        var parentId = await ResolveParentNotificationIdAsync(orderId, userId, cancellationToken);
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            OrderId = orderId,
            ParentNotificationId = parentId,
            Title = title.Trim(),
            Content = content.Trim(),
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Notification>().AddAsync(notification);
    }

    public async Task PublishDeliveryStatusChildAsync(
        Guid orderId,
        Guid userId,
        string orderCode,
        DeliveryState deliveryStatus,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        var title = GetDeliveryStatusTitle(deliveryStatus);
        var content = GetDeliveryStatusBody(orderCode, deliveryStatus, detail);
        await PublishOrderThreadChildAsync(
            orderId,
            userId,
            orderCode,
            title,
            content,
            NotificationType.DeliveryUpdate,
            cancellationToken);
    }

    private async Task<Guid?> ResolveParentNotificationIdAsync(Guid orderId, Guid userId, CancellationToken cancellationToken)
    {
        var roots = await _unitOfWork.Repository<Notification>()
            .FindAsync(n =>
                n.OrderId == orderId
                && n.UserId == userId
                && n.ParentNotificationId == null);

        return roots.OrderBy(n => n.CreatedAt).FirstOrDefault()?.NotificationId;
    }

    private static string GetOrderStatusDisplayName(OrderState status) =>
        status switch
        {
            OrderState.Pending => "Chờ thanh toán",
            OrderState.Paid => "Đã thanh toán",
            OrderState.ReadyToShip => "Sẵn sàng giao",
            OrderState.DeliveredWaitConfirm => "Đã giao — chờ bạn xác nhận",
            OrderState.Completed => "Hoàn thành",
            OrderState.Canceled => "Đã hủy",
            OrderState.Refunded => "Đã hoàn tiền",
            OrderState.Failed => "Giao hàng thất bại",
            _ => status.ToString()
        };

    private static string GetDeliveryStatusTitle(DeliveryState status) =>
        status switch
        {
            DeliveryState.ReadyToShip => "Chuẩn bị giao hàng",
            DeliveryState.PickedUp => "Shipper đã nhận đơn",
            DeliveryState.InTransit => "Đang giao hàng",
            DeliveryState.DeliveredWaitConfirm => "Đã giao đến bạn",
            DeliveryState.Failed => "Giao hàng thất bại",
            DeliveryState.Completed => "Hoàn tất giao hàng",
            _ => "Cập nhật giao hàng"
        };

    private static string GetDeliveryStatusBody(string orderCode, DeliveryState status, string? detail) =>
        status switch
        {
            DeliveryState.ReadyToShip =>
                $"Đơn {orderCode} đang được chuẩn bị giao.",
            DeliveryState.PickedUp =>
                $"Shipper đã nhận giao đơn {orderCode}.",
            DeliveryState.InTransit =>
                $"Shipper đang trên đường giao đơn {orderCode}.",
            DeliveryState.DeliveredWaitConfirm =>
                $"Đơn {orderCode} đã được giao. Vui lòng xác nhận nhận hàng.",
            DeliveryState.Failed =>
                string.IsNullOrWhiteSpace(detail)
                    ? $"Giao đơn {orderCode} không thành công."
                    : $"Giao đơn {orderCode} không thành công. Lý do: {detail}",
            DeliveryState.Completed =>
                $"Bạn đã xác nhận hoàn tất đơn {orderCode}. Cảm ơn bạn!",
            _ =>
                $"Đơn {orderCode}: cập nhật trạng thái giao hàng."
        };
}
