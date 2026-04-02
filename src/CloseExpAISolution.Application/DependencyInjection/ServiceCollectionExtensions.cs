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
using CloseExpAISolution.Application.Mapbox.Extensions;
using CloseExpAISolution.Application.Email.Extensions;
using StackExchange.Redis;
using CloseExpAISolution.Application.Configuration;

namespace CloseExpAISolution.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PickupSearchOptions>(configuration.GetSection(PickupSearchOptions.SectionName));

        services.AddHttpContextAccessor();

        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
            cfg.AddProfile<ProductMappingProfile>();
            cfg.AddProfile<SupermarketMappingProfile>();
            cfg.AddProfile<MarketStaffMappingProfile>();
            cfg.AddProfile<FeedbackMappingProfile>();
            cfg.AddProfile<OrderMappingProfile>();
            cfg.AddProfile<CategoryMappingProfile>();
            cfg.AddProfile<RefundMappingProfile>();
        });

        services.AddScoped<IServiceProviders, CloseExpAISolution.Application.ServiceProviders.ServiceProviders>();

        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
        }

        services.AddAIService(configuration);

        // Register Mapbox Geocoding Service
        services.AddMapboxService(configuration);

        // Register Email + Quartz background jobs
        services.AddEmailServices(configuration);

        // Register Delivery services
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IDeliveryAdminService, DeliveryAdminService>();
        services.AddScoped<IPackagingService, PackagingService>();

        // Register Product workflow services
        services.AddScoped<IR2StorageService, R2StorageService>();
        services.AddScoped<IMarketPriceService, MarketPriceService>();
        services.AddScoped<IBarcodeLookupService, BarcodeLookupService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IProductWorkflowService, ProductWorkflowService>();

        // Register HttpClient for downloading images (bypasses CDN hotlink protection)
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

