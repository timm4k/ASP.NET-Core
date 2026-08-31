using Microsoft.Extensions.Primitives;

namespace MinimalApiMiddleware.Profiles;

internal static class UserProfileQuery
{
    private const int MaximumTextLength = 80;

    public static bool TryParse(
        IQueryCollection query,
        out UserProfile? profile,
        out IReadOnlyList<string> errors)
    {
        List<string> validationErrors = [];
        string name = ReadRequiredText(query, "name", validationErrors);
        string surname = ReadRequiredText(query, "surname", validationErrors);
        string city = ReadRequiredText(query, "city", validationErrors);
        string occupation = ReadRequiredText(query, "occupation", validationErrors);
        string hobby = ReadRequiredText(query, "hobby", validationErrors);
        string favoriteColor = ReadRequiredText(query, "favoriteColor", validationErrors);
        int age = ReadAge(query, validationErrors);

        errors = validationErrors;
        if (validationErrors.Count > 0)
        {
            profile = null;
            return false;
        }

        profile = new UserProfile(name, surname, age, city, occupation, hobby, favoriteColor);
        return true;
    }

    private static string ReadRequiredText(
        IQueryCollection query,
        string key,
        ICollection<string> errors)
    {
        StringValues values = query[key];
        string value = values.ToString().Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"Query parameter '{key}' is required");
            return string.Empty;
        }

        if (values.Count != 1 || value.Length > MaximumTextLength)
        {
            errors.Add($"Query parameter '{key}' must be a single value up to {MaximumTextLength} characters");
            return string.Empty;
        }

        return value;
    }

    private static int ReadAge(IQueryCollection query, ICollection<string> errors)
    {
        StringValues values = query["age"];
        if (values.Count == 1 && int.TryParse(values[0], out int age) && age is >= 1 and <= 120)
        {
            return age;
        }

        errors.Add("Query parameter 'age' must be a whole number from 1 to 120");
        return 0;
    }
}
