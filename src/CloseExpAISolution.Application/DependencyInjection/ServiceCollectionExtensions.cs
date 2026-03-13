using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CloseExpAISolution.Application.AIService.Clients;
using CloseExpAISolution.Application.AIService.Extensions;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.Mappings;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Application.Services;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;

namespace CloseExpAISolution.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
            cfg.AddProfile<ProductMappingProfile>();
            cfg.AddProfile<SupermarketMappingProfile>();
            cfg.AddProfile<MarketStaffMappingProfile>();
            cfg.AddProfile<OrderMappingProfile>();
        });

        services.AddScoped<IServiceProviders, CloseExpAISolution.Application.ServiceProviders.ServiceProviders>();

        services.AddAIService(configuration);

        services.AddHttpClient("ImageDownloader", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient("BarcodeLookup", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "CloseExpAI/1.0");
        });

        return services;
    }
}

