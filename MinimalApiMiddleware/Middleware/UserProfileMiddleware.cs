using MinimalApiMiddleware.Profiles;

namespace MinimalApiMiddleware.Middleware;

internal sealed class UserProfileMiddleware
{
    private readonly RequestDelegate _next;

    public UserProfileMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!MiddlewareRequest.HasPath(context, "/profile"))
        {
            await _next(context);
            return;
        }

        if (!await MiddlewareRequest.RequireGetAsync(context))
        {
            return;
        }

        if (!UserProfileQuery.TryParse(context.Request.Query, out UserProfile? profile, out IReadOnlyList<string> errors))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { errors });
            return;
        }

        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsJsonAsync(new { profile });
    }
}
