using System.Net.Http.Headers;
using CloseExpAISolution.Application.Mapbox.Configuration;
using CloseExpAISolution.Application.Mapbox.Clients;
using CloseExpAISolution.Application.Mapbox.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace CloseExpAISolution.Application.Mapbox.Extensions;

/// <summary>
/// Extension methods for registering Mapbox Service dependencies
/// </summary>
public static class MapboxServiceExtensions
{
    /// <summary>
    /// Add Mapbox Geocoding Service with HttpClient + Polly retry policies
    /// </summary>
    public static IServiceCollection AddMapboxService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var settingsSection = configuration.GetSection(MapboxSettings.SectionName);
        services.Configure<MapboxSettings>(settingsSection);

        var settings = settingsSection.Get<MapboxSettings>() ?? new MapboxSettings();
        settings.Validate();

        // Register HttpClient with resilience policies
        var httpClientBuilder = services.AddHttpClient<IMapboxService, MapboxService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<MapboxSettings>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CloseExpAI-Backend/1.0");
        });

        // Add Polly retry policy
        if (settings.RetryCount > 0)
        {
            httpClientBuilder.AddPolicyHandler(GetRetryPolicy(settings));
        }

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(MapboxSettings settings)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: settings.RetryCount,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(settings.RetryDelayMs * Math.Pow(2, retryAttempt - 1)));
    }
}
