using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace CloseExpAISolution.API.Hubs;

public class JwtSubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var principal = connection.User;
        if (principal == null)
            return null;

        return principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
