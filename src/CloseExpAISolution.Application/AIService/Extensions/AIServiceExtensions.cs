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

public static class AIServiceExtensions
{
    public static IServiceCollection AddAIService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settingsSection = configuration.GetSection(AIServiceSettings.SectionName);
        services.Configure<AIServiceSettings>(settingsSection);

        var settings = settingsSection.Get<AIServiceSettings>() ?? new AIServiceSettings();
        settings.Validate();

        services.AddMemoryCache();

        var httpClientBuilder = services.AddHttpClient<IAIServiceClient, AIServiceClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AIServiceSettings>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add(options.ApiKeyHeader, options.ApiKey);
            }

            client.DefaultRequestHeaders.UserAgent.ParseAdd("CloseExpAI-Backend/1.0");
        });

        if (settings.RetryCount > 0)
        {
            httpClientBuilder.AddPolicyHandler(GetRetryPolicy(settings));
        }

        services.AddScoped<IAIServiceBatchClient>(sp =>
            (AIServiceClient)sp.GetRequiredService<IAIServiceClient>());

        return services;
    }

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
