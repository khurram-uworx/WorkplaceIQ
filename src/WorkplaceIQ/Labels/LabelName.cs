using System.Globalization;
using System.Text;

namespace WorkplaceIQ.Labels;

public sealed record LabelName(string Name, string NormalizedName, string Slug)
{
    public static LabelName From(string value)
    {
        var name = value.Trim();
        var normalized = name.ToUpperInvariant();
        var slug = CreateSlug(name);

        return new LabelName(name, normalized, slug);
    }

    public static IReadOnlyList<LabelName> ParseList(string? labels)
    {
        if (string.IsNullOrWhiteSpace(labels))
        {
            return [];
        }

        return labels
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(From)
            .GroupBy(label => label.NormalizedName, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    public static IReadOnlyList<LabelName> ParseList(IEnumerable<string>? labels)
    {
        if (labels is null)
        {
            return [];
        }

        return labels
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Select(From)
            .GroupBy(label => label.NormalizedName, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    private static string CreateSlug(string name)
    {
        var builder = new StringBuilder(name.Length);
        var previousWasSeparator = false;

        foreach (var character in name.Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        if (builder.Length > 0 && builder[^1] == '-')
        {
            builder.Length--;
        }

        return builder.Length == 0
            ? "label"
            : builder.ToString();
    }
}
