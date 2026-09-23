using AstralObservatory.Data;
using AstralObservatory.Models;

namespace AstralObservatory.Services;

public interface INotificationService
{
    Task<Notification> CreateAsync(string userId, string message, CancellationToken cancellationToken);
}

public sealed class NotificationService(
    AppDbContext db,
    NotificationBroadcaster broadcaster) : INotificationService
{
    public async Task<Notification> CreateAsync(string userId, string message, CancellationToken cancellationToken)
    {
        Notification notification = new()
        {
            UserId = userId,
            Message = message,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);
        await broadcaster.BroadcastAsync(
            userId,
            new NotificationEvent(notification.Id, notification.Message, notification.CreatedAt),
            cancellationToken);
        return notification;
    }
}
