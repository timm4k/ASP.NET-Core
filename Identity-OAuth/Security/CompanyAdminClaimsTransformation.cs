using System.Security.Claims;
using IdentityOAuth.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace IdentityOAuth.Security;

internal sealed class CompanyAdminClaimsTransformation(IOptions<AuthOptions> options)
    : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult(principal);
        }

        ClaimsPrincipal clone = principal.Clone();
        ClaimsIdentity? identity = clone.Identity as ClaimsIdentity;
        string? email = clone.FindFirstValue(ClaimTypes.Email);

        if (identity is not null
            && !clone.IsInRole(Roles.Admin)
            && email?.EndsWith(options.Value.AdminDomain, StringComparison.OrdinalIgnoreCase) == true)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Admin));
        }

        return Task.FromResult(clone);
    }
}
