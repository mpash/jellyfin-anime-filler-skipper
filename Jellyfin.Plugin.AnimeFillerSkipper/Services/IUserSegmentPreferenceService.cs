using System;
using System.Collections.Generic;
using Jellyfin.Plugin.AnimeFillerSkipper.Models;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Services;

public interface IUserSegmentPreferenceService
{
    SegmentPreferenceStatusDto GetUnknownSegmentActionStatus();

    UpdateSegmentPreferenceResult SetUnknownSegmentAction(
        IReadOnlyCollection<Guid>? userIds,
        string action);
}
