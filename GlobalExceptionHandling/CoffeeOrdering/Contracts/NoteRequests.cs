namespace CoffeeOrdering.Contracts;

public sealed record CreateNoteRequest(string? Title, string? Content);

public sealed record UpdateNoteRequest(string? Title, string? Content);
