namespace MinimalApiMiddleware.Middleware;

internal sealed class ViewCounterMiddleware
{
    private readonly RequestDelegate _next;
    private long _viewCount;

    public ViewCounterMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!MiddlewareRequest.HasPath(context, "/counter"))
        {
            await _next(context);
            return;
        }

        if (!await MiddlewareRequest.RequireGetAsync(context))
        {
            return;
        }

        long viewCount = Interlocked.Increment(ref _viewCount);
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsJsonAsync(new { views = viewCount });
    }
}
