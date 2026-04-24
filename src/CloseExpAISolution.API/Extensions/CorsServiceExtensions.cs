namespace CloseExpAISolution.API.Extensions;

public static class CorsServiceExtensions
{
    public static IServiceCollection AddCorsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[]
            {
                "http://localhost:3000",
                "http://localhost:5173",
                "https://www.closeexpire.io.vn"
            };

        var aiServiceUrl = configuration["AIService:BaseUrl"];
        if (!string.IsNullOrEmpty(aiServiceUrl))
        {
            allowedOrigins = allowedOrigins.Append(aiServiceUrl).ToArray();
        }

        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
            {
                builder.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
