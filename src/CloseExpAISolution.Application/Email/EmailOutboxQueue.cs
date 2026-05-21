using System.Threading.Channels;

namespace CloseExpAISolution.Application.Email;

public sealed class EmailOutboxQueue : IEmailOutboxQueue
{
    private readonly Channel<EmailOutboxMessage> _channel =
        Channel.CreateUnbounded<EmailOutboxMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    public ChannelReader<EmailOutboxMessage> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(EmailOutboxMessage message, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(message, cancellationToken);
}
