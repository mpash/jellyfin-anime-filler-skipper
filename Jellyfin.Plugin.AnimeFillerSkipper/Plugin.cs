using System;
using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.AnimeFillerSkipper.Configuration;

namespace Jellyfin.Plugin.AnimeFillerSkipper;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public override string Name => "Anime Filler Skipper";

    public override Guid Id => Guid.Parse("a8f1c3e4-5d6b-4a2e-9f1c-7e8d3a5b6c2f");

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "AnimeFillerSkipper",
                DisplayName = Name,
                EnableInMainMenu = true,
                MenuSection = "server",
                MenuIcon = "skip_next",
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Configuration.configPage.html",
                    GetType().Namespace)
            }
        };
    }
}
