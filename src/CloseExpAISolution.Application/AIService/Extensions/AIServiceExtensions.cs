using System.Net.Http.Headers;
using CloseExpAISolution.Application.AIService.Clients;
using CloseExpAISolution.Application.AIService.Configuration;
using CloseExpAISolution.Application.AIService.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace CloseExpAISolution.Application.AIService.Extensions;

/// <summary>
/// Extension methods for registering AI Service dependencies
/// </summary>
public static class AIServiceExtensions
{
    /// <summary>
    /// Add AI Service client with resilience policies
    /// </summary>
    public static IServiceCollection AddAIService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var settingsSection = configuration.GetSection(AIServiceSettings.SectionName);
        services.Configure<AIServiceSettings>(settingsSection);

        var settings = settingsSection.Get<AIServiceSettings>() ?? new AIServiceSettings();
        settings.Validate();

        // Register memory cache if not already registered
        services.AddMemoryCache();

        // Register HttpClient with resilience policies
        var httpClientBuilder = services.AddHttpClient<IAIServiceClient, AIServiceClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AIServiceSettings>>().Value;
            
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Add API key if configured
            if (!string.IsNullOrEmpty(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add(options.ApiKeyHeader, options.ApiKey);
            }

            // Add user agent
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CloseExpAI-Backend/1.0");
        });

        // Add Polly policies for resilience
        if (settings.RetryCount > 0)
        {
            httpClientBuilder.AddPolicyHandler(GetRetryPolicy(settings));
        }

        // Also register as batch client interface
        services.AddScoped<IAIServiceBatchClient>(sp => 
            (AIServiceClient)sp.GetRequiredService<IAIServiceClient>());

        return services;
    }

    /// <summary>
    /// Create retry policy with exponential backoff
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(AIServiceSettings settings)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: settings.RetryCount,
                sleepDurationProvider: retryAttempt => 
                    TimeSpan.FromMilliseconds(settings.RetryDelayMs * Math.Pow(2, retryAttempt - 1)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // Logging is handled in the client
                });
    }
}
