using CloseExpAISolution.Application.DependencyInjection;
using CloseExpAISolution.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Pass configuration to ApplicationServices for AI Service setup
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAIService");
app.UseAuthorization();
app.MapControllers();

app.Run();
