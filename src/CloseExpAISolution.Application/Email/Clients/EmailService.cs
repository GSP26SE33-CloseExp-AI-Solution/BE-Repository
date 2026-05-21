using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.Json.Serialization;
using CloseExpAISolution.Application.Email.Configuration;
using CloseExpAISolution.Application.Email.Interfaces;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Email.Clients;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        EmailSettings emailSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (!_emailSettings.IsConfigured)
        {
            _logger.LogError(
                "Email not configured (resend={UseResend}, smtpServer={Server}, from={From})",
                _emailSettings.UseResend,
                _emailSettings.SmtpServer,
                _emailSettings.FromEmail);
            throw new InvalidOperationException("Dịch vụ email chưa được cấu hình trên máy chủ.");
        }

        if (_emailSettings.UseResend)
        {
            await SendViaResendAsync(toEmail, subject, body, cancellationToken);
            return;
        }

        await SendViaSmtpAsync(toEmail, subject, body, cancellationToken);
    }

    public async Task SendEmailWithInlineImagesAsync(
        string toEmail,
        string subject,
        string body,
        IReadOnlyCollection<EmailInlineImage> inlineImages,
        CancellationToken cancellationToken = default)
    {
        if (_emailSettings.UseResend)
        {
            _logger.LogWarning(
                "Resend does not support inline images; sending HTML-only to {ToEmail}",
                toEmail);
            await SendViaResendAsync(toEmail, subject, body, cancellationToken);
            return;
        }

        if (!_emailSettings.IsSmtpConfigured || string.IsNullOrWhiteSpace(_emailSettings.FromEmail))
            throw new InvalidOperationException("Dịch vụ email chưa được cấu hình trên máy chủ.");

        try
        {
            using var client = CreateSmtpClient();
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            var htmlView = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);
            foreach (var image in inlineImages)
            {
                var resource = new LinkedResource(new MemoryStream(image.ContentBytes), image.MediaType)
                {
                    ContentId = image.ContentId,
                    TransferEncoding = TransferEncoding.Base64,
                };
                resource.ContentType.Name = $"{image.ContentId}.png";
                resource.ContentLink = new Uri($"cid:{image.ContentId}");
                htmlView.LinkedResources.Add(resource);
            }

            mailMessage.AlternateViews.Add(htmlView);
            await client.SendMailAsync(mailMessage).WaitAsync(cancellationToken);
            _logger.LogInformation("SMTP inline-image email sent to {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send inline-image email to {ToEmail}", toEmail);
            throw;
        }
    }

    private async Task SendViaResendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(EmailHttpClients.Resend);
        var payload = new ResendEmailRequest
        {
            From = FormatFromAddress(),
            To = [toEmail],
            Subject = subject,
            Html = body,
        };

        using var response = await client.PostAsJsonAsync("emails", payload, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Resend email sent to {ToEmail}", toEmail);
            return;
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError(
            "Resend failed for {ToEmail}: HTTP {Status} {Body}",
            toEmail,
            (int)response.StatusCode,
            errorBody);
        throw new InvalidOperationException(
            "Không gửi được email qua Resend. Kiểm tra ResendApiKey và domain người gửi (FromEmail).");
    }

    private async Task SendViaSmtpAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateSmtpClient();
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage).WaitAsync(cancellationToken);
            _logger.LogInformation("SMTP email sent to {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SMTP failed for {ToEmail}. Render FREE blocks ports 587/465 — set EmailSettings__ResendApiKey.",
                toEmail);
            throw;
        }
    }

    private SmtpClient CreateSmtpClient() =>
        new(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
        {
            Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20_000,
        };

    private string FormatFromAddress()
    {
        if (string.IsNullOrWhiteSpace(_emailSettings.FromName))
            return _emailSettings.FromEmail.Trim();

        return $"{_emailSettings.FromName.Trim()} <{_emailSettings.FromEmail.Trim()}>";
    }

    private sealed class ResendEmailRequest
    {
        [JsonPropertyName("from")]
        public string From { get; init; } = string.Empty;

        [JsonPropertyName("to")]
        public List<string> To { get; init; } = [];

        [JsonPropertyName("subject")]
        public string Subject { get; init; } = string.Empty;

        [JsonPropertyName("html")]
        public string Html { get; init; } = string.Empty;
    }
}

internal static class EmailHttpClients
{
    public const string Resend = "Resend";
}
