namespace CloseExpAISolution.Application.Email;

public interface IEmailOutboxQueue
{
    ValueTask EnqueueAsync(EmailOutboxMessage message, CancellationToken cancellationToken = default);
}
