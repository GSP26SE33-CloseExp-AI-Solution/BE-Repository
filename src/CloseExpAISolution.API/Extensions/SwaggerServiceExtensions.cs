using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace CloseExpAISolution.API.Extensions;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CloseExpAISolution API",
                Version = "v1",
                Description = "AI-powered Near-Expiry Food Trading Platform API"
            });

            options.CustomSchemaIds(type => type.FullName);
            options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your token (no 'Bearer' prefix required)"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerPipeline(this IApplicationBuilder app)
    {
        app.UseStaticFiles();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "CloseExpAISolution API v1");
            options.RoutePrefix = "swagger";

            options.EnableFilter();
            options.DocExpansion(DocExpansion.None);
            options.DisplayRequestDuration();
            options.InjectJavascript("/swagger/custom-swagger.js");
        });

        return app;
    }
}
