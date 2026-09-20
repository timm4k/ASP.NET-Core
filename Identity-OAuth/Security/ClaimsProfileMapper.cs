using System.Security.Claims;
using IdentityOAuth.Dtos;

namespace IdentityOAuth.Security;

internal static class ClaimsProfileMapper
{
    public static ClaimsProfileDto Map(ClaimsPrincipal principal) => new(
        principal.Identity?.Name,
        principal.FindFirstValue(ClaimTypes.Email),
        principal.FindFirstValue(CustomClaims.AvatarUrl),
        principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .Order()
            .ToArray());
}
