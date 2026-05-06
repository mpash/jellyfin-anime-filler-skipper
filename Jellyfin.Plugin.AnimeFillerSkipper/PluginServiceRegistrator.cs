using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.AnimeFillerSkipper;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(
        IServiceCollection serviceCollection,
        IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<IFillerDataService, FillerDataService>();
        serviceCollection.AddSingleton<IAnimeFillerListClient, AnimeFillerListClient>();
    }
}
