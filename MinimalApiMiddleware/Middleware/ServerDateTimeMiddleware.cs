namespace MinimalApiMiddleware.Middleware;

internal sealed class ServerDateTimeMiddleware
{
    private readonly RequestDelegate _next;

    public ServerDateTimeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        bool requestsDate = MiddlewareRequest.HasPath(context, "/date");
        bool requestsTime = MiddlewareRequest.HasPath(context, "/time");

        if (!requestsDate && !requestsTime)
        {
            await _next(context);
            return;
        }

        if (!await MiddlewareRequest.RequireGetAsync(context))
        {
            return;
        }

        DateTimeOffset current = DateTimeOffset.Now;
        object response = requestsDate
            ? new { date = current.ToString("yyyy-MM-dd") }
            : new { time = current.ToString("HH:mm:ss") };

        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsJsonAsync(response);
    }
}
