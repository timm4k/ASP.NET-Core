using AstralObservatory.Contracts;
using AstralObservatory.Data;
using AstralObservatory.Models;
using AstralObservatory.Services;
using Microsoft.EntityFrameworkCore;

namespace AstralObservatory.Endpoints;

public static class PushEndpoints
{
    public static void MapPushEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/push").RequireAuthorization("AccessToken");
        group.MapGet("/public-key", GetPublicKeyAsync);
        group.MapPost("/subscribe", SubscribeAsync);
        group.MapPost("/send/{userId}", SendAsync);
        group.MapPost("/broadcast", BroadcastAsync);
    }

    private static async Task<IResult> GetPublicKeyAsync(VapidKeyProvider keys, CancellationToken cancellationToken)
    {
        WebPush.VapidDetails details = await keys.GetAsync(cancellationToken);
        return Results.Ok(new { details.PublicKey });
    }

    private static async Task<IResult> SubscribeAsync(
        PushSubscriptionRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        string? userId = principal.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out Uri? endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(request.P256dh)
            || string.IsNullOrWhiteSpace(request.Auth))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["subscription"] = ["The browser subscription is invalid"] });
        }

        Models.PushSubscription? subscription = await db.PushSubscriptions
            .SingleOrDefaultAsync(item => item.Endpoint == request.Endpoint, cancellationToken);
        if (subscription is null)
        {
            subscription = new Models.PushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.PushSubscriptions.Add(subscription);
        }
        else
        {
            subscription.UserId = userId;
            subscription.P256dh = request.P256dh;
            subscription.Auth = request.Auth;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { subscription.Id });
    }

    private static async Task<IResult> SendAsync(
        string userId,
        PushMessageRequest request,
        ClaimsPrincipal principal,
        IWebPushService push,
        CancellationToken cancellationToken)
    {
        if (principal.GetUserId() != userId)
        {
            return Results.Forbid();
        }

        return ValidateMessage(request, out IResult? error)
            ? Results.Ok(await push.SendToUserAsync(userId, request.Title.Trim(), request.Message.Trim(), cancellationToken))
            : error!;
    }

    private static async Task<IResult> BroadcastAsync(
        PushMessageRequest request,
        IWebPushService push,
        CancellationToken cancellationToken)
    {
        return ValidateMessage(request, out IResult? error)
            ? Results.Ok(await push.BroadcastAsync(request.Title.Trim(), request.Message.Trim(), cancellationToken))
            : error!;
    }

    private static bool ValidateMessage(PushMessageRequest request, out IResult? error)
    {
        if (string.IsNullOrWhiteSpace(request.Title)
            || request.Title.Length > 80
            || string.IsNullOrWhiteSpace(request.Message)
            || request.Message.Length > 240)
        {
            error = Results.ValidationProblem(new Dictionary<string, string[]> { ["message"] = ["Title and message are required and must fit the notification limits"] });
            return false;
        }

        error = null;
        return true;
    }
}
