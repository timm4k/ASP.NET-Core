using CoffeeOrdering.Contracts;
using CoffeeOrdering.Http;
using CoffeeOrdering.Models;
using CoffeeOrdering.Services;

namespace CoffeeOrdering.Endpoints;

public static class AuthEndpoints
{
    private const int MinimumPasswordLength = 8;
    private const int MaximumPasswordLength = 100;
    private const int MaximumUsernameLength = 32;

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder auth = endpoints.MapGroup(string.Empty);

        auth.MapPost("/register", Register);
        auth.MapPost("/login", Login);
        auth.MapPost("/refresh", Refresh);

        return endpoints;
    }

    private static IResult Register(RegisterRequest request, UserStore users, JwtTokenService tokens)
    {
        if (ValidateCredentials(request.Username, request.Password) is { } validation)
        {
            return validation;
        }

        if (!users.TryCreate(request.Username!.Trim(), request.Password!, out AppUser user))
        {
            return ProblemResults.Problem(
                StatusCodes.Status409Conflict,
                "Username is already registered",
                "Choose another username",
                "USERNAME_TAKEN",
                "This username is already in use",
                "Username uniqueness validation failed");
        }

        return Results.Ok(tokens.CreateTokenPair(user));
    }

    private static IResult Login(LoginRequest request, UserStore users, JwtTokenService tokens)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return InvalidCredentials();
        }

        AppUser? user = users.ValidateCredentials(request.Username, request.Password);
        return user is null ? InvalidCredentials() : Results.Ok(tokens.CreateTokenPair(user));
    }

    private static IResult Refresh(
        RefreshRequest request,
        RefreshTokenStore refreshTokens,
        UserStore users,
        JwtTokenService tokens)
    {
        RefreshSession? session = refreshTokens.Consume(request.RefreshToken ?? string.Empty);
        AppUser? user = session is null ? null : users.FindById(session.UserId);

        return user is null
            ? ProblemResults.Problem(
                StatusCodes.Status401Unauthorized,
                "Refresh token is invalid",
                "The refresh token is expired, unknown or was already used",
                "INVALID_REFRESH_TOKEN",
                "Sign in again to continue",
                "Refresh token validation or rotation failed")
            : Results.Ok(tokens.CreateTokenPair(user));
    }

    private static IResult? ValidateCredentials(string? username, string? password)
    {
        Dictionary<string, string[]> errors = [];

        if (string.IsNullOrWhiteSpace(username) || username.Trim().Length > MaximumUsernameLength)
        {
            errors["username"] = [$"Username must contain 1 to {MaximumUsernameLength} characters"];
        }
        else if (username.Any(char.IsWhiteSpace))
        {
            errors["username"] = ["Username cannot contain spaces"];
        }

        if (password is null || password.Length is < MinimumPasswordLength or > MaximumPasswordLength)
        {
            errors["password"] = [$"Password must contain {MinimumPasswordLength} to {MaximumPasswordLength} characters"];
        }

        return errors.Count == 0
            ? null
            : ProblemResults.Validation(
                errors,
                "INVALID_CREDENTIALS_FORMAT",
                "Check the username and password requirements",
                "Registration input did not satisfy credential constraints");
    }

    private static IResult InvalidCredentials() => ProblemResults.Problem(
        StatusCodes.Status401Unauthorized,
        "Authentication failed",
        "The username or password is incorrect",
        "INVALID_CREDENTIALS",
        "Check your username and password",
        "Credentials did not match a registered user");
}
