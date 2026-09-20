using System.Security.Claims;
using IdentityOAuth.Configuration;
using IdentityOAuth.Data;
using IdentityOAuth.Endpoints;
using IdentityOAuth.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

AuthOptions authOptions = builder.Configuration
    .GetRequiredSection(AuthOptions.SectionName)
    .Get<AuthOptions>()
    ?? throw new InvalidOperationException("Authentication configuration is missing");

GoogleOptions googleOptions = builder.Configuration
    .GetRequiredSection(GoogleOptions.SectionName)
    .Get<GoogleOptions>()
    ?? throw new InvalidOperationException("Google authentication configuration is missing");

builder.Services.AddOptions<AuthOptions>()
    .Bind(builder.Configuration.GetRequiredSection(AuthOptions.SectionName))
    .Validate(options => options.IsValid(), "Authentication settings are invalid")
    .ValidateOnStart();

builder.Services.AddOptions<GoogleOptions>()
    .Bind(builder.Configuration.GetRequiredSection(GoogleOptions.SectionName))
    .Validate(options => options.IsValid, "Google ClientId and ClientSecret must be configured together")
    .ValidateOnStart();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOptions<DemoAccountsOptions>()
        .Bind(builder.Configuration.GetRequiredSection(DemoAccountsOptions.SectionName))
        .Validate(options => options.IsValid(), "Demo account settings are invalid")
        .ValidateOnStart();
}

builder.Services.AddDbContext<AppIdentityDbContext>(
    options => options.UseInMemoryDatabase("IdentityOAuth"));

AuthenticationBuilder authentication = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.LoginPath = authOptions.LoginPath;
        options.AccessDeniedPath = authOptions.AccessDeniedPath;
        options.ExpireTimeSpan = authOptions.CookieLifetime;
        options.SlidingExpiration = false;
        options.Cookie.Name = "IdentityOAuth.Authentication";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    })
    .AddCookie(IdentityConstants.ExternalScheme, options =>
    {
        options.ExpireTimeSpan = authOptions.ExternalCookieLifetime;
        options.Cookie.Name = "IdentityOAuth.External";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

if (googleOptions.IsConfigured)
{
    authentication.AddGoogle(options =>
    {
        options.ClientId = googleOptions.ClientId!;
        options.ClientSecret = googleOptions.ClientSecret!;
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.CallbackPath = googleOptions.CallbackPath;
        options.ClaimActions.MapJsonKey(
            CustomClaims.AvatarUrl,
            "picture",
            ClaimValueTypes.String);
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.AdminOnly,
        policy => policy.RequireRole(Roles.Admin));
});

builder.Services
    .AddIdentityCore<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = authOptions.RequireUniqueEmail;
        options.SignIn.RequireConfirmedAccount = false;
        options.Lockout.MaxFailedAccessAttempts = authOptions.LockoutMaxFailedAttempts;
        options.Lockout.DefaultLockoutTimeSpan = authOptions.LockoutDuration;
    })
    .AddEntityFrameworkStores<AppIdentityDbContext>()
    .AddSignInManager();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "IdentityOAuth.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddTransient<IClaimsTransformation, CompanyAdminClaimsTransformation>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.SeedDemoAccountsAsync();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapPageEndpoints();

app.Run();
