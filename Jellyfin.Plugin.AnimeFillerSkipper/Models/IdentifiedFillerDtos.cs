using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Models;

public class IdentifiedFillerResultDto
{
    public IReadOnlyList<IdentifiedFillerSeriesDto> Series { get; set; } = Array.Empty<IdentifiedFillerSeriesDto>();
}

public class IdentifiedFillerSeriesDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public bool HasCachedData { get; set; }

    public DateTime? LastUpdated { get; set; }

    public int FillerCount { get; set; }

    public int MixedCount { get; set; }

    public IReadOnlyList<IdentifiedFillerSeasonDto> Seasons { get; set; } = Array.Empty<IdentifiedFillerSeasonDto>();
}

public class IdentifiedFillerSeasonDto
{
    public int? IndexNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<IdentifiedFillerEpisodeDto> Episodes { get; set; } = Array.Empty<IdentifiedFillerEpisodeDto>();
}

public class IdentifiedFillerEpisodeDto
{
    public Guid Id { get; set; }

    public int? SeasonNumber { get; set; }

    public int? EpisodeNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public long? RunTimeTicks { get; set; }

    public bool HasPrimaryImage { get; set; }

    public string FillerState { get; set; } = IdentifiedFillerStates.None;
}

public static class IdentifiedFillerStates
{
    public const string None = "None";

    public const string Filler = "Filler";

    public const string Mixed = "Mixed";
}
