using CloseExpAISolution.API.Extensions;
using CloseExpAISolution.Application.DependencyInjection;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Data;
using CloseExpAISolution.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Register all services using extension methods for clean organization
builder.Services.AddControllers();
builder.Services
    .AddSwaggerServices()
    .AddAuthenticationServices(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline using extension method
app.UseApplicationPipeline();

app.Run();
