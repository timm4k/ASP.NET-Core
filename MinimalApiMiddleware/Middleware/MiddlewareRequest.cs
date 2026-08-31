namespace MinimalApiMiddleware.Middleware;

internal static class MiddlewareRequest
{
    public static bool HasPath(HttpContext context, string path)
    {
        return context.Request.Path.Equals(path, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<bool> RequireGetAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method))
        {
            return true;
        }

        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        context.Response.Headers.Allow = HttpMethods.Get;
        await context.Response.WriteAsJsonAsync(new { error = "Only GET requests are allowed" });
        return false;
    }
}
