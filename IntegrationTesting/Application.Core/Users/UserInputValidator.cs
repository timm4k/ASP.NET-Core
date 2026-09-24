using System.ComponentModel.DataAnnotations;

namespace Application.Core.Users;

internal static class UserInputValidator
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public static void ValidateCreate(CreateUserRequest request) =>
        Validate(request.Name, request.Email, request.Password, true);

    public static void ValidateUpdate(UpdateUserRequest request) =>
        Validate(request.Name, request.Email, request.Password, request.Password is not null);

    private static void Validate(string name, string email, string? password, bool validatePassword)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length is < 2 or > 80)
        {
            errors[nameof(name)] = ["Name must contain between 2 and 80 characters"];
        }

        if (string.IsNullOrWhiteSpace(email) || email.Length > 254 || !EmailValidator.IsValid(email))
        {
            errors[nameof(email)] = ["Email must be a valid address"];
        }

        if (validatePassword && !IsStrongPassword(password))
        {
            errors[nameof(password)] = ["Password must contain at least 8 characters, an uppercase letter, a lowercase letter and a digit"];
        }

        if (errors.Count > 0)
        {
            throw new InputValidationException(errors);
        }
    }

    private static bool IsStrongPassword(string? password) =>
        password is { Length: >= 8 and <= 128 }
        && password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit);
}
