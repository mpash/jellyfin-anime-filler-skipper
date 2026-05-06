using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Model;

public class ShowFillerData
{
    [JsonPropertyName("showName")]
    public string ShowName { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("fillerEpisodes")]
    public HashSet<int> FillerEpisodes { get; set; } = new();

    [JsonPropertyName("mixedEpisodes")]
    public HashSet<int> MixedEpisodes { get; set; } = new();

    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }

    [JsonPropertyName("etag")]
    public string? Etag { get; set; }

    [JsonPropertyName("itemIds")]
    public HashSet<Guid> ItemIds { get; set; } = new();

    public bool IsFiller(int episodeNumber, bool treatMixedAsFiller)
    {
        return FillerEpisodes.Contains(episodeNumber)
            || (treatMixedAsFiller && MixedEpisodes.Contains(episodeNumber));
    }
}

public class FillerDataStore
{
    [JsonPropertyName("shows")]
    public Dictionary<string, ShowFillerData> Shows { get; set; } = new();

    [JsonPropertyName("slugMap")]
    public Dictionary<Guid, string> ItemToSlug { get; set; } = new();
}

public class FillerListResponse
{
    public string? Html { get; init; }
    public string? Etag { get; init; }
    public bool NotModified { get; init; }
}
