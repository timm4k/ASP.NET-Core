namespace CoffeeOrdering.Models;

public sealed record CoffeeNote(
    Guid Id,
    Guid OwnerId,
    string Title,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
