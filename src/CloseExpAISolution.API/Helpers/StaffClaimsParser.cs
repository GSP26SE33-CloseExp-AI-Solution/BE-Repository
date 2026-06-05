using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CloseExpAISolution.Application.Auth;

namespace CloseExpAISolution.API.Helpers;

public static class StaffClaimsParser
{
    public static (Guid? SupermarketStaffId, Guid? SupermarketId) Read(ClaimsPrincipal user)
    {
        var staffRaw = user.FindFirst(JwtStaffClaims.SupermarketStaffId)?.Value;
        var marketRaw = user.FindFirst(JwtStaffClaims.SupermarketId)?.Value;
        Guid? staffId = Guid.TryParse(staffRaw, out var s) ? s : null;
        Guid? marketId = Guid.TryParse(marketRaw, out var m) ? m : null;
        return (staffId, marketId);
    }

    public static Guid? ReadUserId(ClaimsPrincipal user)
    {
        foreach (var claimType in new[]
                 {
                     ClaimTypes.NameIdentifier,
                     JwtRegisteredClaimNames.Sub,
                     "sub",
                 })
        {
            var value = user.FindFirstValue(claimType);
            if (Guid.TryParse(value, out var id))
                return id;
        }

        return null;
    }
}
