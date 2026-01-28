using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CloseExpAISolution.Application.AIService.Clients;
using CloseExpAISolution.Application.AIService.Extensions;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Application.Services;
using CloseExpAISolution.Application.Services.Interface;

namespace CloseExpAISolution.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        // only register the aggregate service provider; it will new-up inner services
        services.AddScoped<IServiceProviders, CloseExpAISolution.Application.ServiceProviders.ServiceProviders>();

        // Register AI Service
        services.AddAIService(configuration);
        
        // Register HttpClient for downloading images (bypasses CDN hotlink protection)
        services.AddHttpClient("ImageDownloader", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        // Register HttpClient for Barcode Lookup (Open Food Facts API)
        services.AddHttpClient("BarcodeLookup", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "CloseExpAI/1.0");
        });
        
        // Register Barcode Lookup Service
        services.AddSingleton<IBarcodeLookupService, BarcodeLookupService>();
        
        // Register AI Product Service
        services.AddScoped<IAIProductService, AIProductService>();

        return services;
    }
}
