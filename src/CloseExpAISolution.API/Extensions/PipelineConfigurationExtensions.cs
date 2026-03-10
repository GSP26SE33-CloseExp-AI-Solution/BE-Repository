namespace CloseExpAISolution.API.Extensions;

public static class PipelineConfigurationExtensions
{
    public static IApplicationBuilder UseApplicationPipeline(this IApplicationBuilder app)
    {
        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        var enableSwaggerInProd = string.Equals(
            Environment.GetEnvironmentVariable("ENABLE_SWAGGER"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        // Apply Swagger in development (and optionally in production if enabled)
        if (env.IsDevelopment() || enableSwaggerInProd)
        {
            app.UseSwaggerPipeline();
        }

        // Common middleware pipeline
        app.UseHttpsRedirection();
        app.UseRouting();

        // CORS - Enable for frontend and AI Service integration
        app.UseCors("CorsPolicy");

        // Authentication & Authorization - order matters!
        app.UseAuthentication();
        app.UseAuthorization();

        // Map controllers
        app.UseEndpoints(endpoints => endpoints.MapControllers());

        return app;
    }
}
