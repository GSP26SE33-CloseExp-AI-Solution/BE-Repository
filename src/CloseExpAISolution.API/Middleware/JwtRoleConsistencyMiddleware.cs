using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.API.Middleware;

/// <summary>
/// So khớp RoleId trên JWT với DB; nếu Admin đổi vai trò (vd. Vendor → SupermarketStaff) thì 401 + errors chứa role_changed.
/// </summary>
public class JwtRoleConsistencyMiddleware
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

    public JwtRoleConsistencyMiddleware(RequestDelegate next) => _next = next;

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
        var roleIdClaim = context.User.FindFirstValue("RoleId");
        if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(roleIdClaim))
        {
            await _next(context);
            return;
        }

        if (!Guid.TryParse(userIdStr, out var userId) || !int.TryParse(roleIdClaim, out var claimedRoleId))
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

        if (user.RoleId != claimedRoleId)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var payload = ApiResponse<object>.ErrorResponse(
                "Vai trò tài khoản đã thay đổi. Vui lòng làm mới phiên đăng nhập.",
                ["role_changed"]);
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await context.Response.WriteAsync(json);
            return;
        }

        await _next(context);
    }
}
