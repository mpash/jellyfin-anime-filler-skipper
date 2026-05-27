using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Models;

public class SegmentPreferenceUserDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
}

public class SegmentPreferenceStatusDto
{
    public IReadOnlyList<SegmentPreferenceUserDto> Users { get; set; } = Array.Empty<SegmentPreferenceUserDto>();
}

public class UpdateSegmentPreferenceRequest
{
    public IReadOnlyList<Guid>? UserIds { get; set; }

    public string Action { get; set; } = UserSegmentPreferenceActions.AskToSkip;
}

public class UpdateSegmentPreferenceResult
{
    public int UpdatedCount { get; set; }
}

public static class UserSegmentPreferenceActions
{
    public const string None = "None";

    public const string AskToSkip = "AskToSkip";

    public const string Skip = "Skip";
}
