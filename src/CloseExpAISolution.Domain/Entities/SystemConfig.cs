namespace CloseExpAISolution.Domain.Entities;

public class SystemConfig
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
