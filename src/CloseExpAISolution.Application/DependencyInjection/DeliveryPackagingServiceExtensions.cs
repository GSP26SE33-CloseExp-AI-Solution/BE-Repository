using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace CloseExpAISolution.Application.DependencyInjection;

public static class DeliveryPackagingServiceExtensions
{
    public static IServiceCollection AddDeliveryPackagingServices(this IServiceCollection services)
    {
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IDeliveryAdminService, DeliveryAdminService>();
        services.AddScoped<IPackagingService, PackagingService>();
        return services;
    }
}
