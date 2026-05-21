namespace CloseExpAISolution.Application.Email.Configuration
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;

        public string? ResendApiKey { get; set; }

        public bool UseResend => !string.IsNullOrWhiteSpace(ResendApiKey);

        public bool IsSmtpConfigured =>
            !string.IsNullOrWhiteSpace(SmtpServer) &&
            !string.IsNullOrWhiteSpace(SmtpUsername) &&
            !string.IsNullOrWhiteSpace(SmtpPassword) &&
            !SmtpServer.Contains("example.com", StringComparison.OrdinalIgnoreCase);

        public bool IsConfigured =>
            (UseResend && !string.IsNullOrWhiteSpace(FromEmail)) ||
            (IsSmtpConfigured && !string.IsNullOrWhiteSpace(FromEmail));
    }
}
