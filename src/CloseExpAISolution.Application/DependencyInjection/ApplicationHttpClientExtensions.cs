using Microsoft.Extensions.DependencyInjection;

namespace CloseExpAISolution.Application.DependencyInjection;

public static class ApplicationHttpClientExtensions
{
    public static IServiceCollection AddApplicationHttpClients(this IServiceCollection services)
    {
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
