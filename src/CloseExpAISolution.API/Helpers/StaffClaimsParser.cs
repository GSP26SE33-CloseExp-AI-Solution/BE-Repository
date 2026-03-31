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
        var v = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(v, out var id) ? id : null;
    }
}
