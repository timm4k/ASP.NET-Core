using MiniBlog.Models;

namespace MiniBlog.Services;

public static class BlogValidator
{
    public const int MaximumUrlLength = 2048;
    public static IReadOnlyDictionary<string, string[]> ValidatePost(
        string? title,
        string? summary,
        string? content,
        string? imageUrl,
        string? externalUrl,
        string? externalLabel,
        string? author,
        IReadOnlyCollection<int>? categoryIds,
        IReadOnlyCollection<Category> availableCategories)
    {
        Dictionary<string, string[]> errors = new(StringComparer.OrdinalIgnoreCase);

        ValidateLength(errors, "title", title, 3, 120, "Title");
        ValidateLength(errors, "summary", summary, 10, 240, "Summary");
        ValidateLength(errors, "content", content, 20, 5000, "Content");
        ValidateLength(errors, "author", author, 2, 40, "Author");

        string normalizedImageUrl = imageUrl?.Trim() ?? string.Empty;
        bool isLocalImage = normalizedImageUrl.StartsWith("/images/", StringComparison.Ordinal) ||
            normalizedImageUrl.StartsWith("/uploads/", StringComparison.Ordinal);
        bool isRemoteImage = IsHttpUrl(normalizedImageUrl);
        if (normalizedImageUrl.Length > MaximumUrlLength)
        {
            errors["imageUrl"] = [$"Image URL must not exceed {MaximumUrlLength} characters"];
        }
        else if (!isLocalImage && !isRemoteImage)
        {
            errors["imageUrl"] = ["Image must use a local image path or an HTTP URL"];
        }

        string normalizedExternalUrl = externalUrl?.Trim() ?? string.Empty;
        string normalizedExternalLabel = externalLabel?.Trim() ?? string.Empty;
        if (normalizedExternalUrl.Length > MaximumUrlLength)
        {
            errors["externalUrl"] = [$"External URL must not exceed {MaximumUrlLength} characters"];
        }
        else if (normalizedExternalUrl.Length > 0 && !IsHttpUrl(normalizedExternalUrl))
        {
            errors["externalUrl"] = ["External link must use an HTTP URL"];
        }
        if (normalizedExternalUrl.Length > 0 && normalizedExternalLabel.Length is < 2 or > 40)
        {
            errors["externalLabel"] = ["External link label must contain from 2 to 40 characters"];
        }
        if (normalizedExternalUrl.Length == 0 && normalizedExternalLabel.Length > 0)
        {
            errors["externalUrl"] = ["External URL is required when a link label is provided"];
        }

        int[] selectedCategoryIds = categoryIds?.Distinct().ToArray() ?? [];
        if (selectedCategoryIds.Length == 0)
        {
            errors["categoryIds"] = ["Select at least one category"];
        }
        else
        {
            HashSet<int> availableIds = availableCategories.Select(category => category.Id).ToHashSet();
            if (selectedCategoryIds.Any(categoryId => !availableIds.Contains(categoryId)))
            {
                errors["categoryIds"] = ["One or more selected categories do not exist"];
            }
        }

        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> ValidateAuthorKey(string? authorKey)
    {
        Dictionary<string, string[]> errors = new(StringComparer.OrdinalIgnoreCase);
        ValidateLength(errors, "authorKey", authorKey, 32, 128, "Author key");
        return errors;
    }
    public static IReadOnlyDictionary<string, string[]> ValidateCategory(string? name)
    {
        Dictionary<string, string[]> errors = new(StringComparer.OrdinalIgnoreCase);
        ValidateLength(errors, "name", name, 2, 40, "Category name");
        return errors;
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && uri.Scheme is "http" or "https";
    }

    private static void ValidateLength(
        IDictionary<string, string[]> errors,
        string field,
        string? value,
        int minimum,
        int maximum,
        string label)
    {
        int length = value?.Trim().Length ?? 0;
        if (length < minimum || length > maximum)
        {
            errors[field] = [$"{label} must contain from {minimum} to {maximum} characters"];
        }
    }
}
