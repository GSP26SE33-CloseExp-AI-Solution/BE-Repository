using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IRealtimeNotificationPublisher
{
    Task PublishAsync(Notification notification, CancellationToken cancellationToken = default);
    Task PublishManyAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default);
}
