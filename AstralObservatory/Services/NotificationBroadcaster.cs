using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace AstralObservatory.Services;

public sealed record NotificationEvent(int Id, string Message, DateTimeOffset CreatedAt);

public sealed class NotificationBroadcaster
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<NotificationEvent>>> _subscribers = new();

    public async Task BroadcastAsync(string userId, NotificationEvent notification, CancellationToken cancellationToken = default)
    {
        if (!_subscribers.TryGetValue(userId, out ConcurrentDictionary<Guid, Channel<NotificationEvent>>? channels))
        {
            return;
        }

        foreach (Channel<NotificationEvent> channel in channels.Values)
        {
            await channel.Writer.WriteAsync(notification, cancellationToken);
        }
    }

    public async IAsyncEnumerable<NotificationEvent> SubscribeAsync(
        string userId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Guid subscriptionId = Guid.NewGuid();
        Channel<NotificationEvent> channel = Channel.CreateUnbounded<NotificationEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        ConcurrentDictionary<Guid, Channel<NotificationEvent>> channels = _subscribers.GetOrAdd(userId, _ => new());
        channels[subscriptionId] = channel;

        try
        {
            await foreach (NotificationEvent notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return notification;
            }
        }
        finally
        {
            channels.TryRemove(subscriptionId, out _);
            if (channels.IsEmpty)
            {
                _subscribers.TryRemove(userId, out _);
            }
        }
    }
}
