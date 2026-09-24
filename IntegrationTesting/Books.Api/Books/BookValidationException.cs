namespace Books.Api.Books;

public sealed class BookValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more book values are invalid")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
