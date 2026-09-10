using CoffeeOrdering.Configuration;
using CoffeeOrdering.Endpoints;
using CoffeeOrdering.Exceptions;
using CoffeeOrdering.Handlers;
using CoffeeOrdering.Http;
using CoffeeOrdering.Models;
using CoffeeOrdering.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<CoffeeMachineOptions>()
    .Bind(builder.Configuration.GetSection(CoffeeMachineOptions.SectionName))
    .Validate(options => options.WaterCapacityMilliliters > 0, "Water capacity must be positive")
    .Validate(options => options.AutomaticRefillSeconds > 0, "Automatic refill delay must be positive")
    .Validate(options => options.PreparationMilliseconds >= 0, "Preparation delay cannot be negative")
    .Validate(options => options.TimeoutEveryOrders >= 0, "Timeout frequency cannot be negative")
    .ValidateOnStart();

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "JWT issuer is required")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "JWT audience is required")
    .Validate(options => Encoding.UTF8.GetByteCount(options.SecretKey) >= 32, "JWT secret key must contain at least 32 bytes")
    .Validate(options => options.AccessTokenMinutes == 2, "Access token lifetime must be two minutes")
    .Validate(options => options.RefreshTokenDays > 0, "Refresh token lifetime must be positive")
    .ValidateOnStart();

JwtOptions jwt = builder.Configuration.GetRequiredSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing");

builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(5),
            NameClaimType = "username"
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                return ProblemResults.Problem(
                    StatusCodes.Status401Unauthorized,
                    "Authentication is required",
                    "Send a valid access token in the Authorization header",
                    "AUTHENTICATION_REQUIRED",
                    "Sign in to access your private notes",
                    "Bearer authentication did not produce an authenticated user").ExecuteAsync(context.HttpContext);
            },
            OnForbidden = context => ProblemResults.Problem(
                StatusCodes.Status403Forbidden,
                "Access is forbidden",
                "The authenticated user does not satisfy the authorization policy",
                "ACCESS_DENIED",
                "You do not have access to this resource",
                "Authorization policy evaluation failed").ExecuteAsync(context.HttpContext)
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.NotesOwner, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("sub"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<CoffeeMachine>();
builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<RefreshTokenStore>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<NoteStore>();
builder.Services.AddExceptionHandler<CoffeeMachineExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        Exception? exception = context.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception is TimeoutException)
        {
            context.ProblemDetails.Status = StatusCodes.Status503ServiceUnavailable;
            context.ProblemDetails.Title = "Coffee preparation timed out";
            context.ProblemDetails.Detail = "The machine did not finish the drink in time";
            context.ProblemDetails.Type = "/problems/brew-timeout";
            context.ProblemDetails.Extensions["reason"] = "BREW_TIMEOUT";
            context.ProblemDetails.Extensions["localized_message"] = "The barista station is busy. Try the order again";
            context.ProblemDetails.Extensions["developer_message"] = "Coffee preparation exceeded the configured time limit";
            return;
        }

        if (exception is BadHttpRequestException)
        {
            context.ProblemDetails.Status = StatusCodes.Status400BadRequest;
            context.ProblemDetails.Title = "Invalid HTTP request";
            context.ProblemDetails.Detail = "The request body could not be read";
            context.ProblemDetails.Type = "/problems/invalid-request";
            context.ProblemDetails.Extensions["reason"] = "INVALID_REQUEST";
            context.ProblemDetails.Extensions["localized_message"] = "Send a valid JSON request body";
            context.ProblemDetails.Extensions["developer_message"] = "The JSON request body is missing or malformed";
            return;
        }

        if (exception is CoffeeMachineException)
        {
            return;
        }

        if (context.ProblemDetails.Status >= StatusCodes.Status500InternalServerError)
        {
            context.ProblemDetails.Title = "Unexpected server error";
            context.ProblemDetails.Detail = "The request could not be completed";
            context.ProblemDetails.Type = "/problems/internal-error";
            context.ProblemDetails.Extensions["reason"] = "INTERNAL_ERROR";
            context.ProblemDetails.Extensions["localized_message"] = "Something went wrong at the coffee station";
            context.ProblemDetails.Extensions["developer_message"] = "Inspect the server log using the trace identifier";
        }
    };
});

WebApplication app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception switch
    {
        TimeoutException => StatusCodes.Status503ServiceUnavailable,
        BadHttpRequestException badRequest => badRequest.StatusCode,
        _ => StatusCodes.Status500InternalServerError
    }
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapCoffeeEndpoints();
app.MapAuthEndpoints();
app.MapNoteEndpoints();

app.Run();
