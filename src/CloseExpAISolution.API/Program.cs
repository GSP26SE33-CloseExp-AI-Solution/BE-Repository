using CloseExpAISolution.API.Extensions;
using CloseExpAISolution.Application.DependencyInjection;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Data;
using CloseExpAISolution.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services
    .AddSwaggerServices()
    .AddCorsServices(builder.Configuration)
    .AddAuthenticationServices(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

var env = app.Services.GetRequiredService<IWebHostEnvironment>();
var config = app.Services.GetRequiredService<IConfiguration>();
var runMigrations = config.GetValue<bool>("Database:RunMigrationsOnStartup");
var migrationTimeoutSeconds = config.GetValue<int>("Database:MigrationStartupTimeoutSeconds");
if (migrationTimeoutSeconds <= 0) migrationTimeoutSeconds = 90;

if (env.IsDevelopment() && runMigrations)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(migrationTimeoutSeconds));

    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.SetCommandTimeout(TimeSpan.FromSeconds(Math.Min(120, migrationTimeoutSeconds)));
        await context.Database.MigrateAsync(cts.Token);
        await DataSeeder.SeedAsync(context);
        logger.LogInformation("Database migrations and seeding completed.");
    }
    catch (OperationCanceledException)
    {
        logger.LogWarning("Database migration or seeding was cancelled (timeout after {Seconds}s). Start the app without migrations or run: dotnet ef database update --project src/CloseExpAISolution.Infrastructure", migrationTimeoutSeconds);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migrations or seeding failed. You can run migrations manually: dotnet ef database update --project src/CloseExpAISolution.Infrastructure");
    }
}

app.UseApplicationPipeline();

app.Run();
