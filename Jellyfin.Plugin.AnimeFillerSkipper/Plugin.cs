using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.AnimeFillerSkipper.Configuration;

namespace Jellyfin.Plugin.AnimeFillerSkipper;

public class Plugin : BasePlugin<PluginConfiguration>
{
    public override string Name => "Anime Filler Skipper";

    public override Guid Id => Guid.Parse("a8f1c3e4-5d6b-4a2e-9f1c-7e8d3a5b6c2f");

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
    }
}
