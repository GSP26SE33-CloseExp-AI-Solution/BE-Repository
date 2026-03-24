using CloseExpAISolution.Application.AIService.Clients;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.Services;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace CloseExpAISolution.Application.DependencyInjection;

public static class ProductWorkflowServiceExtensions
{
    public static IServiceCollection AddProductWorkflowServices(this IServiceCollection services)
    {
        services.AddScoped<IR2StorageService, R2StorageService>();
        services.AddScoped<IMarketPriceService, MarketPriceService>();
        services.AddScoped<IBarcodeLookupService, BarcodeLookupService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IProductWorkflowService, ProductWorkflowService>();
        return services;
    }
}
