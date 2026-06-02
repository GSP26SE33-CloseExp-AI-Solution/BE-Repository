using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Class;

public class NoOpRealtimeNotificationPublisher : IRealtimeNotificationPublisher
{
    public Task PublishAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _ = notification;
        _ = cancellationToken;
        return Task.CompletedTask;
    }

    public Task PublishManyAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default)
    {
        _ = notifications;
        _ = cancellationToken;
        return Task.CompletedTask;
    }
}
