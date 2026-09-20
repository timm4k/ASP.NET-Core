using IdentityOAuth.Security;

namespace IdentityOAuth.Endpoints;

internal static class PageEndpoints
{
    private static readonly Dictionary<string, string> PublicPages = new()
    {
        ["/"] = "index.html",
        ["/login"] = "login.html",
        ["/register"] = "register.html",
        ["/access-denied"] = "access-denied.html",
    };

    public static void MapPageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dashboard", (IWebHostEnvironment environment) =>
                Results.File(Path.Combine(environment.ContentRootPath, "ProtectedPages", "dashboard.html"), "text/html"))
            .RequireAuthorization();

        endpoints.MapGet("/admin-panel", (IWebHostEnvironment environment) =>
                Results.File(Path.Combine(environment.ContentRootPath, "ProtectedPages", "admin.html"), "text/html"))
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        foreach ((string route, string page) in PublicPages)
        {
            endpoints.MapGet(route, (IWebHostEnvironment environment) =>
                Results.File(Path.Combine(environment.WebRootPath, page), "text/html"));
        }
    }
}
