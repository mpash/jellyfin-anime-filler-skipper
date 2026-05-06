using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Services;

public static class FillerDataParser
{
    private static readonly string[] CategoryPatterns = new[]
    {
        "Manga Canon Episodes:",
        "Mixed Canon/Filler Episodes:",
        "Filler Episodes:"
    };

    private static readonly Regex _categoryBoundaryRegex = new(
        @"(Manga Canon Episodes:|Mixed Canon/Filler Episodes:|Filler Episodes:)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static (HashSet<int> filler, HashSet<int> mixed, string? showName) Parse(
        string html, ILogger logger)
    {
        var filler = new HashSet<int>();
        var mixed = new HashSet<int>();
        string? showName = null;

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            if (titleNode != null)
            {
                showName = titleNode.InnerText
                    .Replace("Filler List | The Ultimate Anime Filler Guide", "")
                    .Trim();
            }

            var rawText = doc.DocumentNode.InnerText
                .Replace('\n', ' ')
                .Replace('\r', ' ');

            var parts = _categoryBoundaryRegex.Split(rawText);

            for (var i = 1; i < parts.Length; i += 2)
            {
                var category = parts[i];
                var data = i + 1 < parts.Length ? parts[i + 1] : string.Empty;

                var episodeData = ExtractEpisodeData(data);
                var episodes = ParseEpisodeNumbers(episodeData);

                if (category.StartsWith("Filler", StringComparison.OrdinalIgnoreCase))
                {
                    filler.UnionWith(episodes);
                }
                else if (category.StartsWith("Mixed", StringComparison.OrdinalIgnoreCase))
                {
                    mixed.UnionWith(episodes);
                }
            }

            if (filler.Count == 0 && mixed.Count == 0)
            {
                filler.UnionWith(ParseFromQuickList(rawText, "Filler Episodes:"));
                mixed.UnionWith(ParseFromQuickList(rawText, "Mixed Canon/Filler Episodes:"));
            }

            logger.LogInformation(
                "Parsed {FillerCount} filler + {MixedCount} mixed episodes for {ShowName}",
                filler.Count, mixed.Count, showName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing filler data from HTML");
        }

        return (filler, mixed, showName);
    }

    internal static string ExtractEpisodeData(string text)
    {
        foreach (var pattern in CategoryPatterns)
        {
            var idx = text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                return text[..idx].Trim();
            }
        }

        return text.Trim();
    }

    internal static HashSet<int> ParseEpisodeNumbers(string data)
    {
        var episodes = new HashSet<int>();
        var parts = data.Split(',');

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (trimmed.Contains('-'))
            {
                var rangeParts = trimmed.Split('-');
                if (rangeParts.Length == 2
                    && int.TryParse(rangeParts[0], out var start)
                    && int.TryParse(rangeParts[1], out var end))
                {
                    for (var i = start; i <= end; i++)
                    {
                        episodes.Add(i);
                    }
                }
            }
            else if (int.TryParse(trimmed, out var num))
            {
                episodes.Add(num);
            }
        }

        return episodes;
    }

    internal static HashSet<int> ParseFromQuickList(string text, string prefix)
    {
        var episodes = new HashSet<int>();

        var parts = _categoryBoundaryRegex.Split(text);
        for (var i = 1; i < parts.Length; i += 2)
        {
            var category = parts[i];
            if (!category.Equals(prefix, StringComparison.OrdinalIgnoreCase)) continue;

            var data = i + 1 < parts.Length ? parts[i + 1] : string.Empty;
            var episodeData = ExtractEpisodeData(data);
            return ParseEpisodeNumbers(episodeData);
        }

        return episodes;
    }
}
