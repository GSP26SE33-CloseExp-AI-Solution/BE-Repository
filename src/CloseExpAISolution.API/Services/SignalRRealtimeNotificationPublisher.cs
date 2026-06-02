using CloseExpAISolution.API.Hubs;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace CloseExpAISolution.API.Services;

public class SignalRRealtimeNotificationPublisher : IRealtimeNotificationPublisher
{
    public const string NotificationCreatedEvent = "notification.received";

    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRRealtimeNotificationPublisher> _logger;

    public SignalRRealtimeNotificationPublisher(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRRealtimeNotificationPublisher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PublishAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        if (notification == null)
            return;

        await PublishInternalAsync(notification, cancellationToken);
    }

    public async Task PublishManyAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default)
    {
        foreach (var notification in notifications)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await PublishInternalAsync(notification, cancellationToken);
        }
    }

    private async Task PublishInternalAsync(Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients.User(notification.UserId.ToString())
                .SendAsync(NotificationCreatedEvent, new
                {
                    notification.NotificationId,
                    notification.UserId,
                    notification.OrderId,
                    notification.ParentNotificationId,
                    notification.Title,
                    notification.Content,
                    notification.Type,
                    notification.IsRead,
                    notification.CreatedAt
                }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to push realtime notification {NotificationId} to user {UserId}",
                notification.NotificationId,
                notification.UserId);
        }
    }
}
