namespace CloseExpAISolution.Application.Payment;

public class PayOsSettings
{
    public const string SectionName = "PayOsSettings";

    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
}
