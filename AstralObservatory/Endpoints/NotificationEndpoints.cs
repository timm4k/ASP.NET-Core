using System.Text.Json;
using AstralObservatory.Configuration;
using AstralObservatory.Contracts;
using AstralObservatory.Data;
using AstralObservatory.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AstralObservatory.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/notifications").RequireAuthorization("AccessToken");
        group.MapGet("/{userId}/unread", GetUnreadAsync);
        group.MapPost("/{userId}/mark-read", MarkReadAsync);
        group.MapGet("/{userId}/long-poll", LongPollAsync);
        group.MapGet("/stream", StreamAsync);
        group.MapPost("/test", CreateTestAsync);
    }

    private static async Task<IResult> GetUnreadAsync(
        string userId,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!IsOwner(principal, userId))
        {
            return Results.Forbid();
        }

        return Results.Ok(await GetUnreadNotificationsAsync(db, userId, cancellationToken));
    }

    private static async Task<IResult> MarkReadAsync(
        string userId,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!IsOwner(principal, userId))
        {
            return Results.Forbid();
        }

        int updated = await db.Notifications
            .Where(item => item.UserId == userId && !item.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsRead, true), cancellationToken);
        return Results.Ok(new { updated });
    }

    private static async Task<IResult> LongPollAsync(
        string userId,
        ClaimsPrincipal principal,
        AppDbContext db,
        IOptions<MonitoringOptions> options,
        CancellationToken cancellationToken)
    {
        if (!IsOwner(principal, userId))
        {
            return Results.Forbid();
        }

        using CancellationTokenSource timeout = new(options.Value.LongPollingTimeout);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            while (!linked.IsCancellationRequested)
            {
                List<NotificationResponse> notifications = await GetUnreadNotificationsAsync(db, userId, linked.Token);
                if (notifications.Count > 0)
                {
                    return Results.Ok(notifications);
                }

                await Task.Delay(options.Value.LongPollingInterval, linked.Token);
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return Results.NoContent();
        }

        return Results.NoContent();
    }

    private static async Task StreamAsync(
        HttpContext context,
        NotificationBroadcaster broadcaster,
        CancellationToken cancellationToken)
    {
        string? userId = context.User.GetUserId();
        if (userId is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Append("X-Accel-Buffering", "no");

        await foreach (NotificationEvent notification in broadcaster.SubscribeAsync(userId, cancellationToken))
        {
            string json = JsonSerializer.Serialize(notification, JsonSerializerOptions.Web);
            await context.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }

    private static async Task<IResult> CreateTestAsync(
        TestNotificationRequest request,
        ClaimsPrincipal principal,
        INotificationService notifications,
        IWebPushService push,
        CancellationToken cancellationToken)
    {
        string? userId = principal.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        string message = string.IsNullOrWhiteSpace(request.Message)
            ? "A new astronomical event was detected"
            : request.Message.Trim();
        if (message.Length > 240)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["message"] = ["Message cannot exceed 240 characters"] });
        }

        Models.Notification notification = await notifications.CreateAsync(userId, message, cancellationToken);
        PushDeliveryResult delivery = await push.SendToUserAsync(
            userId,
            "Astral Observatory",
            message,
            cancellationToken);
        return Results.Ok(new
        {
            notification.Id,
            notification.Message,
            notification.CreatedAt,
            Push = delivery
        });
    }

    private static Task<List<NotificationResponse>> GetUnreadNotificationsAsync(
        AppDbContext db,
        string userId,
        CancellationToken cancellationToken) =>
        db.Notifications
            .AsNoTracking()
            .Where(item => item.UserId == userId && !item.IsRead)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new NotificationResponse(item.Id, item.Message, item.IsRead, item.CreatedAt))
            .ToListAsync(cancellationToken);

    private static bool IsOwner(ClaimsPrincipal principal, string userId) => principal.GetUserId() == userId;

    private sealed record NotificationResponse(int Id, string Message, bool IsRead, DateTimeOffset CreatedAt);
}
