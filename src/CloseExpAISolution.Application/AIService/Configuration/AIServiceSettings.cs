namespace CloseExpAISolution.Application.AIService.Configuration;

public class AIServiceSettings
{
    public const string SectionName = "AIService"; public string BaseUrl { get; set; } = "http://localhost:8000";
    public string? ApiKey { get; set; }
    public string ApiKeyHeader { get; set; } = "X-API-Key";
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public bool EnableCircuitBreaker { get; set; } = true;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
    public bool EnableCaching { get; set; } = true;
    public int PricingCacheDurationMinutes { get; set; } = 5;
    public int MaxImageSizeMB { get; set; } = 10;
    public bool EnableLogging { get; set; } = true;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new ArgumentException("AIService:BaseUrl is required");

        if (TimeoutSeconds <= 0)
            throw new ArgumentException("AIService:TimeoutSeconds must be positive");

        if (RetryCount < 0)
            throw new ArgumentException("AIService:RetryCount cannot be negative");

        if (MaxImageSizeMB <= 0)
            throw new ArgumentException("AIService:MaxImageSizeMB must be positive");
    }
}
