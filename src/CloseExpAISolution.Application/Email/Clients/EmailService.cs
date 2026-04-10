using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using CloseExpAISolution.Application.Email.Interfaces;
using CloseExpAISolution.Application.Email.Configuration;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Email.Clients
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(EmailSettings emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage).WaitAsync(cancellationToken);
                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                throw;
            }
        }

        public async Task SendEmailWithInlineImagesAsync(
            string toEmail,
            string subject,
            string body,
            IReadOnlyCollection<EmailInlineImage> inlineImages,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                    Subject = subject,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                var htmlView = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);
                foreach (var image in inlineImages)
                {
                    var resource = new LinkedResource(new MemoryStream(image.ContentBytes), image.MediaType)
                    {
                        ContentId = image.ContentId,
                        TransferEncoding = TransferEncoding.Base64
                    };
                    resource.ContentType.Name = $"{image.ContentId}.png";
                    resource.ContentLink = new Uri($"cid:{image.ContentId}");
                    htmlView.LinkedResources.Add(resource);
                }

                mailMessage.AlternateViews.Add(htmlView);

                await client.SendMailAsync(mailMessage).WaitAsync(cancellationToken);
                _logger.LogInformation("Email with inline images sent successfully to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send inline-image email to {ToEmail}", toEmail);
                throw;
            }
        }
    }
}