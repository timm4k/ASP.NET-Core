using MinimalApiMiddleware.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["ServerUrl"] ?? "http://127.0.0.1:5184");

WebApplication app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseMiddleware<ViewCounterMiddleware>();
app.UseMiddleware<ServerDateTimeMiddleware>();
app.UseMiddleware<ObjectGalleryMiddleware>();
app.UseMiddleware<RequestHeadersMiddleware>();
app.UseMiddleware<UserProfileMiddleware>();

app.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    await context.Response.WriteAsJsonAsync(new { error = "Endpoint not found" });
});

app.Run();
