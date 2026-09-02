using System.Globalization;
using System.Text;

namespace MiniBlog.Services;

public static class SlugGenerator
{
    public static string Create(string value)
    {
        string normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        StringBuilder slug = new();
        bool separatorPending = false;

        foreach (char character in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }
            if (char.IsLetterOrDigit(character))
            {
                if (separatorPending && slug.Length > 0)
                {
                    slug.Append('-');
                }
                slug.Append(character);
                separatorPending = false;
            }
            else
            {
                separatorPending = slug.Length > 0;
            }
        }

        string result = slug.ToString().Trim('-');
        return result.Length == 0 ? "untitled" : result;
    }
}
