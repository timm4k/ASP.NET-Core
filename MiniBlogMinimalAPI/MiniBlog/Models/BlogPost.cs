namespace MiniBlog.Models;

public sealed record BlogPost(
    int Id,
    string Title,
    string Slug,
    string Summary,
    string Content,
    string ImageUrl,
    string ExternalUrl,
    string ExternalLabel,
    string Author,
    string AuthorKeyHash,
    bool IsActive,
    IReadOnlyList<int> CategoryIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
