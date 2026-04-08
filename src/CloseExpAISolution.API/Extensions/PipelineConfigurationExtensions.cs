using CloseExpAISolution.API.Middleware;

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

        if (env.IsDevelopment() || enableSwaggerInProd)
        {
            app.UseSwaggerPipeline();
        }

        if (!env.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        app.UseRouting();

        app.UseCors("CorsPolicy");

        app.UseAuthentication();
        app.UseMiddleware<JwtRoleConsistencyMiddleware>();
        app.UseMiddleware<UserAccountActiveMiddleware>();
        app.UseAuthorization();

        app.UseEndpoints(endpoints => endpoints.MapControllers());

        return app;
    }
}
