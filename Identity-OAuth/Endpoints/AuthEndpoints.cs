using System.Net.Mail;
using System.Security.Claims;
using IdentityOAuth.Configuration;
using IdentityOAuth.Dtos;
using IdentityOAuth.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IdentityOAuth.Endpoints;

internal static class AuthEndpoints
{
    private const string GoogleProvider = "Google";
    private const string GoogleChallengePath = "/auth/google";
    private const string GoogleCallbackPath = "/signin-google-callback";
    public static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/antiforgery/token", IssueAntiforgeryToken);

        endpoints.MapPost("/register", RegisterAsync)
            .AddEndpointFilter<AntiforgeryValidationFilter>();

        endpoints.MapPost("/login", LoginAsync)
            .AddEndpointFilter<AntiforgeryValidationFilter>();

        endpoints.MapPost("/logout", LogoutAsync)
            .AddEndpointFilter<AntiforgeryValidationFilter>();

        IWebHostEnvironment environment =
            endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (environment.IsDevelopment())
        {
            endpoints.MapPost("/demo-login", DemoLoginAsync)
                .AddEndpointFilter<AntiforgeryValidationFilter>();
        }

        endpoints.MapGet(GoogleChallengePath, BeginGoogleSignIn);
        endpoints.MapGet(GoogleCallbackPath, CompleteGoogleSignInAsync);
    }

    private static IResult IssueAntiforgeryToken(
        HttpContext context,
        IAntiforgery antiforgery)
    {
        AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
        return Results.Ok(new
        {
            token = tokens.RequestToken,
            headerName = tokens.HeaderName
        });
    }

    private static async Task<IResult> RegisterAsync(
        RegisterDto dto,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IOptions<AuthOptions> options)
    {
        if (ValidateRegistration(dto) is { } validation)
        {
            return validation;
        }

        string email = dto.Email!.Trim();
        IdentityUser user = new()
        {
            UserName = email,
            Email = email
        };

        IdentityResult result = await userManager.CreateAsync(user, dto.Password!);
        if (!result.Succeeded)
        {
            return IdentityValidationProblem(result);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Ok(new { redirect = options.Value.DefaultRedirectPath });
    }

    private static async Task<IResult> LoginAsync(
        LoginDto dto,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IOptions<AuthOptions> options)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrEmpty(dto.Password))
        {
            return InvalidCredentials();
        }

        IdentityUser? user = await userManager.FindByEmailAsync(dto.Email.Trim());
        if (user is null)
        {
            return InvalidCredentials();
        }

        SignInResult result = await signInManager.PasswordSignInAsync(
            user,
            dto.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        return result.Succeeded
            ? Results.Ok(new { redirect = options.Value.DefaultRedirectPath })
            : InvalidCredentials();
    }

    private static async Task<IResult> LogoutAsync(
        SignInManager<IdentityUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.Ok(new { redirect = "/" });
    }

    private static async Task<IResult> DemoLoginAsync(
        DemoLoginDto dto,
        IOptions<DemoAccountsOptions> demoAccounts,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IOptions<AuthOptions> authOptions)
    {
        DemoAccount? account = demoAccounts.Value.Accounts.FirstOrDefault(
            candidate => string.Equals(
                candidate.Key,
                dto.AccountKey,
                StringComparison.OrdinalIgnoreCase));

        if (account is null)
        {
            return Results.NotFound(new { error = "Demo account was not found" });
        }

        IdentityUser? user = await userManager.FindByEmailAsync(account.Email);
        if (user is null)
        {
            return Results.NotFound(new { error = "Demo account is unavailable" });
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Ok(new { redirect = authOptions.Value.DefaultRedirectPath });
    }

    private static IResult BeginGoogleSignIn(
        SignInManager<IdentityUser> signInManager,
        IOptions<GoogleOptions> google,
        IOptions<AuthOptions> auth)
    {
        if (!google.Value.IsConfigured)
        {
            return Results.Redirect(BuildErrorPath(auth.Value.LoginPath, "google"));
        }

        AuthenticationProperties properties =
            signInManager.ConfigureExternalAuthenticationProperties(
                GoogleProvider,
                GoogleCallbackPath);

        return Results.Challenge(properties, [GoogleProvider]);
    }

    private static async Task<IResult> CompleteGoogleSignInAsync(
        HttpContext context,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AuthOptions> options)
    {
        string errorPath = BuildErrorPath(options.Value.LoginPath, "external");

        ExternalLoginInfo? info = await signInManager.GetExternalLoginInfoAsync();
        string? email = info?.Principal.FindFirstValue(ClaimTypes.Email);
        if (info is null || string.IsNullOrWhiteSpace(email))
        {
            return Results.Redirect(errorPath);
        }

        IdentityUser? user = await userManager.FindByLoginAsync(
            info.LoginProvider,
            info.ProviderKey);

        bool shouldLinkLogin = user is null;
        bool createdUser = false;

        if (user is null)
        {
            user = await userManager.FindByEmailAsync(email);
        }

        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            IdentityResult created = await userManager.CreateAsync(user);
            if (!created.Succeeded)
            {
                return Results.Redirect(errorPath);
            }

            createdUser = true;
        }

        if (shouldLinkLogin)
        {
            IdentityResult linked = await userManager.AddLoginAsync(user, info);
            if (!linked.Succeeded)
            {
                if (createdUser)
                {
                    await userManager.DeleteAsync(user);
                }

                return Results.Redirect(errorPath);
            }
        }

        await UpdateAvatarClaimAsync(userManager, user, info.Principal);
        await signInManager.SignInAsync(user, isPersistent: false);
        await context.SignOutAsync(IdentityConstants.ExternalScheme);

        return Results.Redirect(options.Value.DefaultRedirectPath);
    }

    private static async Task UpdateAvatarClaimAsync(
        UserManager<IdentityUser> userManager,
        IdentityUser user,
        ClaimsPrincipal externalPrincipal)
    {
        string? avatarUrl = externalPrincipal.FindFirstValue(CustomClaims.AvatarUrl);
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            return;
        }

        IList<Claim> claims = await userManager.GetClaimsAsync(user);
        Claim? current = claims.FirstOrDefault(
            claim => claim.Type == CustomClaims.AvatarUrl);

        if (current?.Value == avatarUrl)
        {
            return;
        }

        IdentityResult result = current is null
            ? await userManager.AddClaimAsync(
                user,
                new Claim(CustomClaims.AvatarUrl, avatarUrl))
            : await userManager.ReplaceClaimAsync(
                user,
                current,
                new Claim(CustomClaims.AvatarUrl, avatarUrl));

        if (!result.Succeeded)
        {
            throw new InvalidOperationException("The Google avatar claim could not be stored");
        }
    }

    private static IResult? ValidateRegistration(RegisterDto dto)
    {
        Dictionary<string, string[]> errors = [];

        if (string.IsNullOrWhiteSpace(dto.Email)
            || dto.Email.Length > IdentityRules.MaximumEmailLength
            || !MailAddress.TryCreate(dto.Email.Trim(), out _))
        {
            errors["email"] = ["Enter a valid email address"];
        }

        if (dto.Password is null
            || dto.Password.Length is < IdentityRules.MinimumPasswordLength
                or > IdentityRules.MaximumPasswordLength)
        {
            errors["password"] =
            [
                $"Password must contain {IdentityRules.MinimumPasswordLength} to {IdentityRules.MaximumPasswordLength} characters"
            ];
        }

        return errors.Count == 0
            ? null
            : Results.ValidationProblem(errors);
    }

    private static IResult IdentityValidationProblem(IdentityResult result)
    {
        string[] errors = result.Errors
            .Select(error => error.Description)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return Results.ValidationProblem(
            new Dictionary<string, string[]> { ["account"] = errors });
    }

    private static IResult InvalidCredentials() => Results.Json(
        new { error = "Invalid email or password" },
        statusCode: StatusCodes.Status401Unauthorized);

    private static string BuildErrorPath(string loginPath, string error) =>
        $"{loginPath}{QueryString.Create("error", error)}";
}
