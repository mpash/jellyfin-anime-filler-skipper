using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public int CacheExpirationHours { get; set; } = 72;

    public bool TreatMixedAsFiller { get; set; } = true;

    public int RequestDelayMs { get; set; } = 2000;
}
