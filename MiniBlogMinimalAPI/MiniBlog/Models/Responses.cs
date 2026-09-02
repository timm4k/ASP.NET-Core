namespace MiniBlog.Models;

public sealed record CategoryResponse(
    int Id,
    string Name,
    string Slug,
    int PostCount,
    string PostsUrl);

public sealed record PostResponse(
    int Id,
    string Title,
    string Slug,
    string Summary,
    string Content,
    string ImageUrl,
    string ExternalUrl,
    string ExternalLabel,
    string Author,
    bool IsActive,
    IReadOnlyList<CategoryResponse> Categories,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Url);

public sealed record CommandResult<T>(
    T? Value,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsSuccess => Errors.Count == 0;

    public static CommandResult<T> Success(T value)
    {
        return new CommandResult<T>(value, new Dictionary<string, string[]>());
    }

    public static CommandResult<T> Invalid(IReadOnlyDictionary<string, string[]> errors)
    {
        return new CommandResult<T>(default, errors);
    }
}
