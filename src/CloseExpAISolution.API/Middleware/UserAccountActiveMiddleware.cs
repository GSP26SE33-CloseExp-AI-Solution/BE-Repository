using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.API.Middleware;

/// <summary>
/// Chặn Bearer token khi User.Status không còn Active (lock/ban/delete/...) — bổ sung cho JwtRoleConsistencyMiddleware.
/// </summary>
public class UserAccountActiveMiddleware
{
    private static readonly string[] SkipPathPrefixes =
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh-token",
        "/api/auth/google-login",
        "/api/auth/verify-otp",
        "/api/auth/resend-otp",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/auth/request-unlock",
        "/swagger"
    };

    private readonly RequestDelegate _next;

    public UserAccountActiveMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        if (SkipPathPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            await _next(context);
            return;
        }

        var user = await unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            await _next(context);
            return;
        }

        if (user.Status == UserState.Active)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        var payload = ApiResponse<object>.ErrorResponse(
            "Tài khoản không còn hoạt động. Vui lòng đăng nhập lại hoặc liên hệ hỗ trợ.",
            ["account_inactive"]);
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await context.Response.WriteAsync(json);
    }
}
