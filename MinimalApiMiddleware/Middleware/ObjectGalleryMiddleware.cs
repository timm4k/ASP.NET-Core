using System.Net;
using System.Text;
using MinimalApiMiddleware.Content;

namespace MinimalApiMiddleware.Middleware;

internal sealed class ObjectGalleryMiddleware
{
    private readonly RequestDelegate _next;

    public ObjectGalleryMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!MiddlewareRequest.HasPath(context, "/objects"))
        {
            await _next(context);
            return;
        }

        if (!await MiddlewareRequest.RequireGetAsync(context))
        {
            return;
        }

        StringBuilder html = new();
        foreach (DisplayObject item in ObjectCatalog.All)
        {
            html.Append("<article class=\"object-card\"><span>")
                .Append(WebUtility.HtmlEncode(item.Category))
                .Append("</span><h3>")
                .Append(WebUtility.HtmlEncode(item.Name))
                .Append("</h3><p>")
                .Append(WebUtility.HtmlEncode(item.Description))
                .Append("</p></article>");
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html.ToString());
    }
}
