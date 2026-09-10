namespace CoffeeOrdering.Models;

public sealed record RefreshSession(Guid UserId, DateTimeOffset ExpiresAt);
