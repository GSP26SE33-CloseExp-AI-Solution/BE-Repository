namespace CloseExpAISolution.API.Extensions;

public static class PipelineConfigurationExtensions
{
    public static IApplicationBuilder UseApplicationPipeline(this IApplicationBuilder app)
    {
        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

        // Apply Swagger in development
        if (env.IsDevelopment())
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
