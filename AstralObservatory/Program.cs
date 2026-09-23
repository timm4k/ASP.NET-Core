using AstralObservatory.Configuration;
using AstralObservatory.Data;
using AstralObservatory.Endpoints;
using AstralObservatory.Infrastructure;
using AstralObservatory.Models;
using AstralObservatory.Repositories;
using AstralObservatory.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<ApplicationOptions>()
    .BindConfiguration(ApplicationOptions.SectionName)
    .Validate(options => options.IsValid(), "Application configuration is invalid")
    .ValidateOnStart();
builder.Services.AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .Validate(options => options.IsValid(), "Database configuration is invalid")
    .ValidateOnStart();
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(options => options.IsValid(), "JWT configuration is invalid")
    .ValidateOnStart();
builder.Services.AddOptions<MonitoringOptions>()
    .BindConfiguration(MonitoringOptions.SectionName)
    .Validate(options => options.IsValid(), "Monitoring configuration is invalid")
    .ValidateOnStart();
builder.Services.AddOptions<SmsOptions>()
    .BindConfiguration(SmsOptions.SectionName)
    .Validate(options => options.IsValid(), "SMS configuration is invalid")
    .ValidateOnStart();

DatabaseOptions databaseOptions = builder.Configuration
    .GetRequiredSection(DatabaseOptions.SectionName)
    .Get<DatabaseOptions>() ?? throw new InvalidOperationException("Database configuration is missing");
string dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AstralObservatory");
Directory.CreateDirectory(dataDirectory);
string connectionString = $"Data Source={Path.Combine(dataDirectory, databaseOptions.FileName)}";
string keyDirectory = Path.Combine(dataDirectory, "keys");
Directory.CreateDirectory(keyDirectory);
IDataProtectionBuilder dataProtection = builder.Services
    .AddDataProtection()
    .SetApplicationName("AstralObservatory")
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
if (OperatingSystem.IsWindows())
{
    dataProtection.ProtectKeysWithDpapi();
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString), ServiceLifetime.Scoped);
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
        options.Stores.ProtectPersonalData = true;
    })
    .AddSignInManager()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddPersonalDataProtection<IdentityLookupProtector, IdentityLookupProtectorKeyRing>();

JwtSigningKey signingKey = new();
builder.Services.AddSingleton(signingKey);
JwtOptions jwtOptions = builder.Configuration
    .GetRequiredSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration is missing");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey.Key,
            ClockSkew = TimeSpan.FromSeconds(15),
            NameClaimType = "name"
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PendingTwoFactor", policy =>
        policy.RequireAuthenticatedUser().RequireClaim("token_type", "2fa_pending"));
    options.AddPolicy("AccessToken", policy =>
        policy.RequireAuthenticatedUser().RequireClaim("token_type", "access"));
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
SmsOptions smsOptions = builder.Configuration
    .GetRequiredSection(SmsOptions.SectionName)
    .Get<SmsOptions>() ?? throw new InvalidOperationException("SMS configuration is missing");
if (smsOptions.IsTwilio)
{
    builder.Services.AddScoped<ISmsService, TwilioSmsService>();
}
else
{
    builder.Services.AddScoped<ISmsService, DevelopmentSmsService>();
}
builder.Services.AddScoped<IEmailService, LoggingEmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IWebPushService, WebPushService>();
builder.Services.AddScoped<IAstralMonitor, AstralMonitor>();
builder.Services.AddScoped<ICelestialObjectRepository, CelestialObjectRepository>();
builder.Services.AddScoped<IObservationRepository, ObservationRepository>();
builder.Services.AddScoped<IObservationAlertRepository, ObservationAlertRepository>();
builder.Services.AddSingleton<NotificationBroadcaster>();
builder.Services.AddScoped<VapidKeyProvider>();
builder.Services.AddHostedService<AstralMonitoringService>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapNotificationEndpoints();
app.MapPushEndpoints();
app.MapObservatoryEndpoints();
app.MapGet("/api/health", () => Results.Ok(new { status = "online", time = DateTimeOffset.UtcNow }));

await app.InitializeDatabaseAsync();
await app.RunAsync();
