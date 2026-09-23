using System.Net;
using System.Text.Json;
using AstralObservatory.Data;
using AstralObservatory.Models;
using Microsoft.EntityFrameworkCore;
using WebPush;
using BrowserPushSubscription = WebPush.PushSubscription;

namespace AstralObservatory.Services;

public sealed class VapidKeyProvider(IDbContextFactory<AppDbContext> contextFactory)
{
    private const string PublicKeyName = "VapidPublicKey";
    private const string PrivateKeyName = "VapidPrivateKey";
    private readonly SemaphoreSlim _lock = new(1, 1);
    private VapidDetails? _cached;

    public async Task<VapidDetails> GetAsync(CancellationToken cancellationToken)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cached is not null)
            {
                return _cached;
            }

            await using AppDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            Dictionary<string, string> values = await db.SystemSettings
                .Where(item => item.Key == PublicKeyName || item.Key == PrivateKeyName)
                .ToDictionaryAsync(item => item.Key, item => item.Value, cancellationToken);

            if (!values.TryGetValue(PublicKeyName, out string? publicKey)
                || !values.TryGetValue(PrivateKeyName, out string? privateKey))
            {
                VapidDetails generated = VapidHelper.GenerateVapidKeys();
                publicKey = generated.PublicKey;
                privateKey = generated.PrivateKey;
                db.SystemSettings.AddRange(
                    new SystemSetting { Key = PublicKeyName, Value = publicKey },
                    new SystemSetting { Key = PrivateKeyName, Value = privateKey });
                await db.SaveChangesAsync(cancellationToken);
            }

            _cached = new VapidDetails("mailto:observatory@localhost", publicKey, privateKey);
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }
}

public sealed record PushDeliveryResult(int Sent, int Removed, int Failed);

public interface IWebPushService
{
    Task<PushDeliveryResult> SendToUserAsync(string userId, string title, string message, CancellationToken cancellationToken);
    Task<PushDeliveryResult> BroadcastAsync(string title, string message, CancellationToken cancellationToken);
}

public sealed class WebPushService(
    AppDbContext db,
    VapidKeyProvider keyProvider,
    ILogger<WebPushService> logger) : IWebPushService
{
    public Task<PushDeliveryResult> SendToUserAsync(string userId, string title, string message, CancellationToken cancellationToken) =>
        SendAsync(db.PushSubscriptions.Where(item => item.UserId == userId), title, message, cancellationToken);

    public Task<PushDeliveryResult> BroadcastAsync(string title, string message, CancellationToken cancellationToken) =>
        SendAsync(db.PushSubscriptions, title, message, cancellationToken);

    private async Task<PushDeliveryResult> SendAsync(
        IQueryable<Models.PushSubscription> query,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        List<Models.PushSubscription> subscriptions = await query.ToListAsync(cancellationToken);
        if (subscriptions.Count == 0)
        {
            return new PushDeliveryResult(0, 0, 0);
        }

        VapidDetails keys = await keyProvider.GetAsync(cancellationToken);
        string payload = JsonSerializer.Serialize(new { title, message, url = "/observatory.html" });
        using WebPushClient client = new();
        int sent = 0;
        int removed = 0;
        int failed = 0;

        foreach (Models.PushSubscription subscription in subscriptions)
        {
            try
            {
                BrowserPushSubscription browserSubscription = new(subscription.Endpoint, subscription.P256dh, subscription.Auth);
                await client.SendNotificationAsync(browserSubscription, payload, keys, cancellationToken);
                sent++;
            }
            catch (WebPushException exception) when (exception.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
            {
                db.PushSubscriptions.Remove(subscription);
                removed++;
            }
            catch (WebPushException exception)
            {
                logger.LogWarning(exception, "Web Push delivery failed for subscription {SubscriptionId}", subscription.Id);
                failed++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Web Push transport failed for subscription {SubscriptionId}", subscription.Id);
                failed++;
            }
        }

        if (removed > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return new PushDeliveryResult(sent, removed, failed);
    }
}
