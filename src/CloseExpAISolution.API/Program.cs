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

// Add CORS for AI Service integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAIService", policy =>
    {
        var aiServiceUrl = builder.Configuration["AIService:BaseUrl"];
        if (!string.IsNullOrEmpty(aiServiceUrl))
        {
            policy.WithOrigins(aiServiceUrl)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CloseExpAISolution API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAIService");
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// Authentication & Authorization middleware - order matters!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
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
