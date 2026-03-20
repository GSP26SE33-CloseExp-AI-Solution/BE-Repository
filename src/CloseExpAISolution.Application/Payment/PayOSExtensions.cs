using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloseExpAISolution.Application.Payment;

public static class PayOSExtensions
{
    public static IServiceCollection AddPayOS(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PayOsSettings>(configuration.GetSection(PayOsSettings.SectionName));
        services.AddScoped<IPaymentService, PaymentService>();
        return services;
    }
}
