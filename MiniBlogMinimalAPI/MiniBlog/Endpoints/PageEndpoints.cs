namespace MiniBlog.Endpoints;

public static class PageEndpoints
{
    private static readonly string[] ApplicationPages =
    [
        "/",
        "/read/{slug}",
        "/posts/add",
        "/my-posts",
        "/posts/edit",
        "/posts/delete",
        "/categories/add",
        "/categories/edit",
        "/categories/delete"
    ];

    public static IEndpointRouteBuilder MapPageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        foreach (string route in ApplicationPages)
        {
            endpoints.MapGet(route, (IWebHostEnvironment environment) =>
                Results.File(Path.Combine(environment.WebRootPath, "index.html"), "text/html"));
        }

        endpoints.MapGet("/about", (IWebHostEnvironment environment) =>
            Results.File(Path.Combine(environment.WebRootPath, "about.html"), "text/html"));

        return endpoints;
    }
}
