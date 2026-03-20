namespace CloseExpAISolution.Application.Payment;

/// <summary>
/// PayOS merchant credentials. Bind section <c>PayOsSettings</c> from configuration.
/// </summary>
public class PayOsSettings
{
    public const string SectionName = "PayOsSettings";

    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
}
