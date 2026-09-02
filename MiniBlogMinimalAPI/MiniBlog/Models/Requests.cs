namespace MiniBlog.Models;

public sealed record PostCreateRequest(
    string? Title,
    string? Summary,
    string? Content,
    string? ImageUrl,
    string? ExternalUrl,
    string? ExternalLabel,
    string? Author,
    string? AuthorKey,
    int[]? CategoryIds);

public sealed record PostEditRequest(
    int Id,
    string? Title,
    string? Summary,
    string? Content,
    string? ImageUrl,
    string? ExternalUrl,
    string? ExternalLabel,
    string? Author,
    int[]? CategoryIds);

public sealed record PostActivationRequest(int Id, string? AuthorKey, bool IsActive);

public sealed record CategoryCreateRequest(string? Name);

public sealed record CategoryEditRequest(int Id, string? Name);
