namespace MinimalApiMiddleware.Middleware;

internal sealed class RequestHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public RequestHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!MiddlewareRequest.HasPath(context, "/headers"))
        {
            await _next(context);
            return;
        }

        if (!await MiddlewareRequest.RequireGetAsync(context))
        {
            return;
        }

        Dictionary<string, string> headers = context.Request.Headers
            .OrderBy(header => header.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                header => header.Key,
                header => header.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsJsonAsync(new { headers });
    }
}
