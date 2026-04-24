using CloseExpAISolution.API.Extensions;
using CloseExpAISolution.Application.DependencyInjection;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Data;
using CloseExpAISolution.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.ValidateCriticalConfiguration();

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
var runSeeding = config.GetValue<bool>("Database:RunSeedingOnStartup");
var migrationTimeoutSeconds = config.GetValue<int>("Database:MigrationStartupTimeoutSeconds");
if (migrationTimeoutSeconds <= 0) migrationTimeoutSeconds = 90;

if (env.IsDevelopment() && (runMigrations || runSeeding))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(migrationTimeoutSeconds));

    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (runMigrations)
        {
            context.Database.SetCommandTimeout(TimeSpan.FromSeconds(Math.Min(120, migrationTimeoutSeconds)));
            await context.Database.MigrateAsync(cts.Token);
        }

        if (runSeeding)
        {
            await DataSeeder.SeedAsync(context);
        }

        logger.LogInformation(
            "Database startup tasks completed. Migrations: {RunMigrations}, Seeding: {RunSeeding}",
            runMigrations,
            runSeeding);
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
