namespace CoffeeOrdering.Contracts;

public sealed record NoteResponse(
    Guid Id,
    string Title,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
