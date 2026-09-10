using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CoffeeOrdering.Http;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        string? value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out Guid userId)
            ? userId
            : throw new InvalidOperationException("The authenticated user identifier is missing");
    }
}
