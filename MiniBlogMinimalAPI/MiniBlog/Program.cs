using MiniBlog.Endpoints;
using MiniBlog.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<BlogRepository>();
builder.Services.AddSingleton<ImageStorage>();

WebApplication app = builder.Build();
app.UseExceptionHandler();
app.UseStaticFiles();

BlogRepository repository = app.Services.GetRequiredService<BlogRepository>();
await repository.InitializeAsync();

app.MapBlogEndpoints();
app.MapPageEndpoints();

app.Run();
