using CloseExpAISolution.Application.Email.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Email;

public sealed class EmailOutboxBackgroundService : BackgroundService
{
    private readonly EmailOutboxQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailOutboxBackgroundService> _logger;

    public EmailOutboxBackgroundService(
        EmailOutboxQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailOutboxBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendEmailAsync(
                    message.ToEmail,
                    message.Subject,
                    message.HtmlBody,
                    stoppingToken);
                _logger.LogInformation("Outbox email delivered to {ToEmail}", message.ToEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox email failed for {ToEmail}", message.ToEmail);
            }
        }
    }
}
