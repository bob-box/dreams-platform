using System.Globalization;
using System.Text;

namespace Borderland.Dreams.Utils;

/// <summary>
/// Generates URL-friendly slugs from cloud names.
/// </summary>
public static class CloudSlugGenerator
{
    private const int MaximumLength = 128;
    
    /// <summary>
    /// Generates a lowercase, ASCII-only slug from the specified name,
    /// replacing non-alphanumeric characters with separators and limiting the result to 128 characters.
    /// </summary>
    /// <param name="name">The name to convert into a slug.</param>
    /// <returns>
    /// A normalized slug containing lowercase letters, digits, and hyphens,
    /// or <c>cloud</c> when the name is null, empty, whitespace, or produces no valid characters.
    /// </returns>
    public static string Generate(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "cloud";

        var normalizedName = name.Normalize(NormalizationForm.FormD);
        var slug = new StringBuilder(MaximumLength);
        var separatorPending = false;

        foreach (var character in normalizedName)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsAsciiLetterOrDigit(character))
            {
                if (separatorPending && slug.Length > 0)
                {
                    if (slug.Length >= MaximumLength - 1)
                        break;

                    slug.Append('-');
                }

                if (slug.Length >= MaximumLength)
                    break;

                slug.Append(char.ToLowerInvariant(character));
                separatorPending = false;
            }
            else if (slug.Length > 0)
            {
                separatorPending = true;
            }
        }

        return slug.Length == 0 ? "cloud" : slug.ToString();
    }
}