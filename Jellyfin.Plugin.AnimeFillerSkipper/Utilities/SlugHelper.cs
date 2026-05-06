using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Utilities;

public static class SlugHelper
{
    private static readonly Regex _invalidChars = new(@"[^a-z0-9\s-]", RegexOptions.Compiled);
    private static readonly Regex _multipleHyphens = new(@"-+", RegexOptions.Compiled);

    public static string GenerateSlug(string seriesName)
    {
        if (string.IsNullOrWhiteSpace(seriesName)) return string.Empty;

        var slug = seriesName.ToLowerInvariant().Trim();
        slug = _invalidChars.Replace(slug, "");
        slug = slug.Replace(' ', '-');
        slug = _multipleHyphens.Replace(slug, "-");
        slug = slug.Trim('-');

        return slug;
    }
}
