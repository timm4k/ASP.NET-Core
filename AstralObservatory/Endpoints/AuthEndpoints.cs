using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AstralObservatory.Configuration;
using AstralObservatory.Contracts;
using AstralObservatory.Models;
using AstralObservatory.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using QRCoder;

namespace AstralObservatory.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/register", RegisterAsync);
        app.MapPost("/api/users", RegisterAsync);
        app.MapPost("/login", LoginAsync);

        RouteGroupBuilder pending = app.MapGroup("/2fa").RequireAuthorization("PendingTwoFactor");
        pending.MapPost("/sms", SendSmsAsync);
        pending.MapPost("/sms/verify", VerifySmsAsync);
        pending.MapPost("/totp/setup", SetupTotpAsync);
        pending.MapPost("/totp/verify", VerifyTotpAsync);

        app.MapGet("/profile", ProfileAsync).RequireAuthorization("AccessToken");
        app.MapPost("/2fa/disable", DisableTwoFactorAsync).RequireAuthorization("AccessToken");
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager)
    {
        Dictionary<string, string[]> errors = ValidateRegistration(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        ApplicationUser user = new()
        {
            Name = request.Name.Trim(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            PhoneNumberConfirmed = true
        };

        IdentityResult result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return Results.ValidationProblem(ToValidationErrors(result));
        }

        return Results.Created($"/profile", new { user.Id, user.Name, user.Email });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService tokens)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return InvalidCredentials();
        }

        SignInResult result = await signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (!result.Succeeded)
        {
            return InvalidCredentials();
        }

        IssuedToken token = tokens.CreatePendingToken(user);
        return Results.Ok(new LoginResponse(token.Value, token.ExpiresAt, ["sms", "totp"]));
    }

    private static async Task<IResult> SendSmsAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ISmsService smsService,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await GetUserAsync(principal, userManager);
        if (user?.PhoneNumber is null)
        {
            return Results.Problem("A phone number is required for SMS authentication", statusCode: 400);
        }

        string code = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);
        try
        {
            SmsDeliveryResult delivery = await smsService.SendCodeAsync(user.PhoneNumber, code, cancellationToken);
            string message = smsService.DeliveryMode == "twilio"
                ? "Verification code sent by SMS"
                : "Development SMS emulation created a verification code";
            return Results.Ok(new SmsChallengeResponse(
                message,
                smsService.DeliveryMode,
                delivery.MaskedDestination,
                delivery.DevelopmentCode));
        }
        catch (SmsDeliveryException exception)
        {
            return Results.Problem(
                title: exception.Message,
                detail: "Check the SMS provider configuration and try again",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["reason"] = "SMS_DELIVERY_FAILED" });
        }
    }

    private static async Task<IResult> VerifySmsAsync(
        SmsVerificationRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        IJwtTokenService tokens)
    {
        ApplicationUser? user = await GetUserAsync(principal, userManager);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        bool valid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultPhoneProvider,
            NormalizeCode(request.Code));

        return valid
            ? await CompleteAuthenticationAsync(user, TwoFactorMethod.Sms, userManager, tokens)
            : Results.ValidationProblem(new Dictionary<string, string[]> { ["code"] = ["The SMS code is invalid or expired"] });
    }

    private static async Task<IResult> SetupTotpAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        IOptions<ApplicationOptions> options)
    {
        ApplicationUser? user = await GetUserAsync(principal, userManager);
        if (user?.Email is null)
        {
            return Results.Unauthorized();
        }

        string? key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        string uri = BuildAuthenticatorUri(options.Value.Name, user.Email, key!);
        using QRCodeGenerator generator = new();
        using QRCodeData data = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        PngByteQRCode qrCode = new(data);
        string dataUrl = $"data:image/png;base64,{Convert.ToBase64String(qrCode.GetGraphic(8))}";
        return Results.Ok(new TotpSetupResponse(FormatKey(key!), uri, dataUrl));
    }

    private static async Task<IResult> VerifyTotpAsync(
        TotpVerificationRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        IJwtTokenService tokens)
    {
        ApplicationUser? user = await GetUserAsync(principal, userManager);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        bool valid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            NormalizeCode(request.Code));

        if (!valid)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["code"] = ["The authenticator code is invalid"] });
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        return await CompleteAuthenticationAsync(user, TwoFactorMethod.Totp, userManager, tokens);
    }

    private static async Task<IResult> ProfileAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        IOptions<SmsOptions> smsOptions)
    {
        ApplicationUser? user = await GetUserAsync(principal, userManager);
        return user is null
            ? Results.Unauthorized()
            : Results.Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                PhoneNumber = MaskPhoneNumber(user.PhoneNumber),
                user.TwoFactorEnabled,
                TwoFactorMethod = user.PreferredTwoFactorMethod?.ToString().ToLowerInvariant(),
                user.EmailAlertsEnabled,
                SmsDeliveryMode = smsOptions.Value.IsTwilio ? "twilio" : "development",
                EmailDeliveryMode = "development"
            });
    }

    private static async Task<IResult> DisableTwoFactorAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        ApplicationUser? user = await GetUserAsync(principal, userManager);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
        user.PreferredTwoFactorMethod = null;
        await userManager.UpdateAsync(user);
        return Results.NoContent();
    }

    private static async Task<IResult> CompleteAuthenticationAsync(
        ApplicationUser user,
        TwoFactorMethod method,
        UserManager<ApplicationUser> userManager,
        IJwtTokenService tokens)
    {
        user.PreferredTwoFactorMethod = method;
        user.TwoFactorEnabled = true;
        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Results.Problem("Two-factor authentication could not be saved", statusCode: 500);
        }

        IssuedToken token = tokens.CreateAccessToken(user);
        return Results.Ok(new TokenResponse(token.Value, token.ExpiresAt));
    }

    private static Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal, UserManager<ApplicationUser> userManager)
    {
        string? userId = principal.GetUserId();
        return userId is null ? Task.FromResult<ApplicationUser?>(null) : userManager.FindByIdAsync(userId);
    }

    private static Dictionary<string, string[]> ValidateRegistration(RegisterRequest request)
    {
        Dictionary<string, string[]> errors = [];
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length is < 2 or > 100)
        {
            errors["name"] = ["Name must contain 2 to 100 characters"];
        }

        if (!new EmailAddressAttribute().IsValid(request.Email))
        {
            errors["email"] = ["Enter a valid email address"];
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber)
            || !Regex.IsMatch(request.PhoneNumber.Trim(), @"^\+[1-9]\d{7,14}$", RegexOptions.CultureInvariant))
        {
            errors["phoneNumber"] = ["Use international format such as +380501234567"];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = ["Password is required"];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ToValidationErrors(IdentityResult result) =>
        result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());

    private static IResult InvalidCredentials() =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["credentials"] = ["Email or password is incorrect"] });

    private static string NormalizeCode(string code) => code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);

    private static string? MaskPhoneNumber(string? phoneNumber) =>
        string.IsNullOrWhiteSpace(phoneNumber) ? null : DevelopmentSmsService.MaskPhoneNumber(phoneNumber);

    private static string BuildAuthenticatorUri(string issuer, string email, string key) =>
        string.Create(CultureInfo.InvariantCulture, $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6");

    private static string FormatKey(string key)
    {
        StringBuilder result = new();
        for (int index = 0; index < key.Length; index += 4)
        {
            if (result.Length > 0)
            {
                result.Append(' ');
            }

            result.Append(key.AsSpan(index, Math.Min(4, key.Length - index)));
        }

        return result.ToString().ToLowerInvariant();
    }
}
