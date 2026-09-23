using Microsoft.EntityFrameworkCore;
using ResponseCaching.Configuration;
using ResponseCaching.Data;
using ResponseCaching.Endpoints;
using ResponseCaching.Infrastructure;
using ResponseCaching.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .Validate(options => options.IsValid(), "Database configuration is invalid")
    .ValidateOnStart();
builder.Services.AddOptions<CachingOptions>()
    .BindConfiguration(CachingOptions.SectionName)
    .Validate(options => options.IsValid(), "Caching configuration is invalid")
    .ValidateOnStart();

DatabaseOptions databaseOptions = builder.Configuration
    .GetRequiredSection(DatabaseOptions.SectionName)
    .Get<DatabaseOptions>() ?? throw new InvalidOperationException("Database configuration is missing");
string dataDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "ResponseCaching");
Directory.CreateDirectory(dataDirectory);
string connectionString = $"Data Source={Path.Combine(dataDirectory, databaseOptions.FileName)}";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddMemoryCache();
builder.Services.AddScoped<PostCacheService>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapPostEndpoints();

await app.InitializeDatabaseAsync();
await app.RunAsync();
