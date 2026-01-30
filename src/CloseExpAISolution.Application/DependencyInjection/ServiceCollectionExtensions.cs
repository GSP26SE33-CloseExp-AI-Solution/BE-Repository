using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CloseExpAISolution.Application.Mappings;
using CloseExpAISolution.Application.ServiceProviders;

namespace CloseExpAISolution.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        // Register AutoMapper với tất cả profiles
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
            cfg.AddProfile<ProductMappingProfile>();
            cfg.AddProfile<SupermarketMappingProfile>();
            cfg.AddProfile<MarketStaffMappingProfile>();
        });

        // only register the aggregate service provider; it will new-up inner services
        services.AddScoped<IServiceProviders, CloseExpAISolution.Application.ServiceProviders.ServiceProviders>();

        return services;
    }
}

