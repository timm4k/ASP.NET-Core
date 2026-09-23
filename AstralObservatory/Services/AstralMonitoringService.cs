using AstralObservatory.Configuration;
using AstralObservatory.Data;
using AstralObservatory.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AstralObservatory.Services;

public sealed record MonitoringResult(int UpdatedObjects, int TriggeredAlerts, DateTimeOffset CompletedAt);

public interface IAstralMonitor
{
    Task<MonitoringResult> RunAsync(CancellationToken cancellationToken);
}

public sealed class AstralMonitor(
    AppDbContext db,
    INotificationService notifications,
    IWebPushService push,
    IEmailService email,
    IOptions<MonitoringOptions> options,
    ILogger<AstralMonitor> logger) : IAstralMonitor
{
    public async Task<MonitoringResult> RunAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<ObservationAlert> restingAlerts = await db.ObservationAlerts
            .Where(alert => !alert.IsActive && alert.ReactivateAt != null)
            .ToListAsync(cancellationToken);
        foreach (ObservationAlert alert in restingAlerts.Where(alert => alert.ReactivateAt <= now))
        {
            alert.IsActive = true;
            alert.TriggeredAt = null;
            alert.ReactivateAt = null;
        }

        List<CelestialObject> objects = await db.CelestialObjects.ToListAsync(cancellationToken);
        foreach (CelestialObject item in objects)
        {
            double next = Math.Clamp(item.CurrentActivity + Random.Shared.NextDouble() * 16 - 8, 0, 100);
            item.CurrentActivity = Math.Round(next, 1);
            item.LastUpdated = now;
            db.Observations.Add(new Observation
            {
                CelestialObjectId = item.Id,
                ActivityLevel = item.CurrentActivity,
                ObservationType = item.ActivityMetric,
                ObservedAt = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        List<ObservationAlert> alerts = await db.ObservationAlerts
            .Include(alert => alert.User)
            .Include(alert => alert.CelestialObject)
            .Where(alert => alert.IsActive)
            .ToListAsync(cancellationToken);

        int triggered = 0;
        foreach (ObservationAlert alert in alerts.Where(alert => alert.CelestialObject.CurrentActivity >= alert.TargetValue))
        {
            string message = $"{alert.CelestialObject.Name} {alert.CelestialObject.ActivityMetric.ToLowerInvariant()} reached {alert.CelestialObject.CurrentActivity:F1}";
            await notifications.CreateAsync(alert.UserId, message, cancellationToken);
            await push.SendToUserAsync(alert.UserId, "Astral Observatory", message, cancellationToken);
            if (alert.User.EmailAlertsEnabled)
            {
                await email.SendObservationAlertAsync(alert.User, alert.CelestialObject, alert.CelestialObject.CurrentActivity, cancellationToken);
            }

            alert.IsActive = false;
            alert.TriggeredAt = now;
            alert.ReactivateAt = now.Add(options.Value.AlertReactivationDelay);
            triggered++;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Astral monitoring updated {ObjectCount} objects and triggered {AlertCount} alerts", objects.Count, triggered);
        return new MonitoringResult(objects.Count, triggered, now);
    }
}

public sealed class AstralMonitoringService(
    IServiceScopeFactory scopeFactory,
    IOptions<MonitoringOptions> options,
    ILogger<AstralMonitoringService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(options.Value.Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                IAstralMonitor monitor = scope.ServiceProvider.GetRequiredService<IAstralMonitor>();
                await monitor.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Astral monitoring cycle failed");
            }
        }
    }
}
