using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using CoffeeOrdering.Models;

namespace CoffeeOrdering.Services;

public sealed class RefreshTokenStore(TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<string, RefreshSession> _sessions = new(StringComparer.Ordinal);

    public void Store(string token, RefreshSession session)
    {
        RemoveExpired();
        _sessions[Hash(token)] = session;
    }

    public RefreshSession? Consume(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !_sessions.TryRemove(Hash(token), out RefreshSession? session))
        {
            return null;
        }

        return session.ExpiresAt > timeProvider.GetUtcNow() ? session : null;
    }

    private void RemoveExpired()
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        foreach ((string key, RefreshSession session) in _sessions)
        {
            if (session.ExpiresAt <= now)
            {
                _sessions.TryRemove(key, out _);
            }
        }
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
