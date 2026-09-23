using System.Security.Claims;

namespace AstralObservatory.Services;

public static class UserContext
{
    public static string? GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier);
}
