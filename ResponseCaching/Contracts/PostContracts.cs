namespace ResponseCaching.Contracts;

public sealed record UpdatePostRequest(string Title, string Content);

public sealed record PostResponse(int Id, string Title, string Content, DateTimeOffset UpdatedAt);
