using Microsoft.Extensions.DependencyInjection;
using CloseExpAISolution.Application.ServiceProviders;

namespace CloseExpAISolution.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        // only register the aggregate service provider; it will new-up inner services
        services.AddScoped<IServiceProviders, CloseExpAISolution.Application.ServiceProviders.ServiceProviders>();

        return services;
    }
}
