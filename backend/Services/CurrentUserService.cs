using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CalorieDiary.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor)
{
    public int? GetUserId()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var userIdValue = user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(userIdValue, out var userId)
            ? userId
            : null;
    }
}
