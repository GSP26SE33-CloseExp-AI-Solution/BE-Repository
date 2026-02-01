using CloseExpAISolution.API.Extensions;
using CloseExpAISolution.Application.DependencyInjection;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Data;
using CloseExpAISolution.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register all services using extension methods for clean organization
builder.Services.AddControllers();
builder.Services
    .AddSwaggerServices()
    .AddAuthenticationServices(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
    await DataSeeder.SeedAsync(context);
}

// Configure the HTTP request pipeline using extension method
app.UseApplicationPipeline();

app.Run();
