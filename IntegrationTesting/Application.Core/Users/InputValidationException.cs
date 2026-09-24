namespace Application.Core.Users;

public sealed class InputValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more input values are invalid")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
