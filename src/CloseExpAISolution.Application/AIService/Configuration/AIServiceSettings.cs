namespace CloseExpAISolution.Application.AIService.Configuration;

/// <summary>
/// Configuration settings for AI Service integration
/// </summary>
public class AIServiceSettings
{
    public const string SectionName = "AIService";

    /// <summary>
    /// Base URL of the AI Service (e.g., http://localhost:8000)
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8000";

    /// <summary>
    /// API Key for authentication (optional in development)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Header name for API Key
    /// </summary>
    public string ApiKeyHeader { get; set; } = "X-API-Key";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for transient failures
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Delay between retries in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Enable circuit breaker pattern
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Number of failures before circuit opens
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Duration circuit stays open in seconds
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Enable response caching
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache duration for pricing responses in minutes
    /// </summary>
    public int PricingCacheDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum image size in MB
    /// </summary>
    public int MaxImageSizeMB { get; set; } = 10;

    /// <summary>
    /// Enable request/response logging
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Validate settings
    /// </summary>
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
