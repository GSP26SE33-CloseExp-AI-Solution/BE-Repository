namespace CloseExpAISolution.Application.Email.Interfaces
{
    public sealed class EmailInlineImage
    {
        public required string ContentId { get; init; }
        public required byte[] ContentBytes { get; init; }
        public required string MediaType { get; init; }
    }

    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
        Task SendEmailWithInlineImagesAsync(
            string toEmail,
            string subject,
            string body,
            IReadOnlyCollection<EmailInlineImage> inlineImages,
            CancellationToken cancellationToken = default);
    }
}