using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Services.Interface;

/// <summary>
/// Pushes in-app notifications for an order: one root row when the order is placed,
/// then child rows linked via parent notification id for status / delivery updates.
/// </summary>
public interface IOrderNotificationPublisher
{
    Task PublishOrderPlacedAsync(Guid orderId, Guid userId, string orderCode, CancellationToken cancellationToken = default);

    Task PublishOrderStatusChangedAsync(
        Guid orderId,
        Guid userId,
        string orderCode,
        OrderState newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Customer-facing update tied to an order thread (e.g. packaging, delivery, failure).
    /// </summary>
    Task PublishOrderThreadChildAsync(
        Guid orderId,
        Guid userId,
        string orderCode,
        string title,
        string content,
        NotificationType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Customer-facing delivery milestone (shipper nhận đơn, đang giao, đã giao, thất bại, hoàn tất).
    /// </summary>
    Task PublishDeliveryStatusChildAsync(
        Guid orderId,
        Guid userId,
        string orderCode,
        DeliveryState deliveryStatus,
        string? detail = null,
        CancellationToken cancellationToken = default);
}
