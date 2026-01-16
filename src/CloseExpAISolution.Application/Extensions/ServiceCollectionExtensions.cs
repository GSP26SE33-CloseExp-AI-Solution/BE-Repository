using Microsoft.Extensions.DependencyInjection;

namespace CloseExpAISolution.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add AutoMapper
        // services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Add FluentValidation
        // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Register application services here
        // services.AddScoped<IProductService, ProductService>();
        // services.AddScoped<IOrderService, OrderService>();
        // services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
