using IdentityOAuth.Configuration;
using IdentityOAuth.Dtos;
using IdentityOAuth.Security;
using Microsoft.Extensions.Options;

namespace IdentityOAuth.Endpoints;

internal static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/app-info", (
            IWebHostEnvironment environment,
            IOptions<GoogleOptions> google,
            IOptions<DemoAccountsOptions> demoAccounts) =>
        {
            object[] accounts = environment.IsDevelopment()
                ? demoAccounts.Value.Accounts
                    .Select(account => new
                    {
                        account.Key,
                        account.Label,
                        account.Email,
                        account.Description
                    })
                    .Cast<object>()
                    .ToArray()
                : [];

            return Results.Ok(new
            {
                googleConfigured = google.Value.IsConfigured,
                googleCallbackPath = google.Value.CallbackPath,
                demoAccounts = accounts
            });
        });

        endpoints.MapGet("/me", (HttpContext context) =>
        {
            ClaimsProfileDto profile = ClaimsProfileMapper.Map(context.User);
            return Results.Ok(profile);
        }).RequireAuthorization();

        endpoints.MapGet("/api/admin/profile", (
            HttpContext context,
            IOptions<AuthOptions> authOptions) =>
        {
            ClaimsProfileDto profile = ClaimsProfileMapper.Map(context.User);
            return Results.Ok(new
            {
                access = "granted",
                email = profile.Email,
                roles = profile.Roles,
                adminDomain = authOptions.Value.AdminDomain,
                cookieLifetimeMinutes = authOptions.Value.CookieLifetime.TotalMinutes
            });
        }).RequireAuthorization(AuthorizationPolicies.AdminOnly);
    }
}
