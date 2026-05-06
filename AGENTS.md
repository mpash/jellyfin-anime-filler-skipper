Be extremely concise, sacrifice grammar for the sake of concision.

## Build commands
- `dotnet build` — compile
- `dotnet test` — run tests (xUnit)
- `dotnet publish -c Release -o publish` — publish for Jellyfin plugin install

## Project structure
```
Jellyfin.Plugin.AnimeFillerSkipper/     # Plugin project (net9.0)
  Plugin.cs                             # Entry point, BasePlugin<PluginConfiguration>
  PluginServiceRegistrator.cs           # DI registration (IFillerDataService, IAnimeFillerListClient)
  Configuration/PluginConfiguration.cs  # Settings
  Model/FillerEpisodeData.cs            # ShowFillerData (IsFiller check), FillerDataStore
  Services/
    AnimeFillerListClient.cs            # HTTP client for animefillerlist.com
    FillerDataParser.cs                 # HTML→filler episode numbers, Regex.Split by categories
    FillerDataService.cs                # JSON file cache layer, thread-safe with SemaphoreSlim
  Providers/FillerSegmentProvider.cs    # IMediaSegmentProvider — full-episode filler segments
  ScheduledTasks/UpdateFillerDataTask.cs# IScheduledTask, daily 4AM, iterates library Series
  Utilities/SlugHelper.cs               # Series name → animefillerlist.com slug

tests/Jellyfin.Plugin.AnimeFillerSkipper.Tests/  # xUnit + Moq
```

## Key interfaces used
- `IMediaSegmentProvider` (MediaBrowser.Controller.MediaSegments) — provides filler segments
- `IScheduledTask` (MediaBrowser.Model.Tasks) — scheduled filler data refresh
- `IPluginServiceRegistrator` — DI registration hook
- `ILibraryManager` — querying library items

## Dependencies
- Jellyfin 10.11.x packages (Common, Controller, Model, Database.Implementations)
- HtmlAgilityPack (bundled — not excluded from runtime)
- Tests: xUnit, Moq

## Filler data flow
1. `UpdateFillerDataTask` → `ILibraryManager.GetItemList(InternalItemsQuery)` → iterate Series
2. `SlugHelper.GenerateSlug(name)` → `AnimeFillerListClient.FetchShowPageAsync(slug)`
3. `FillerDataParser.Parse(html)` → Regex.Split on category boundaries → `ParseEpisodeNumbers`
4. `FillerDataService.StoreFillerDataAsync(slug, data)` → JSON file
5. Playback → `FillerSegmentProvider.GetMediaSegments` → check episode.IndexNumber against cache
6. If filler → `MediaSegmentDto` with Type=Unknown, StartTicks=0, EndTicks=RunTimeTicks

## Segment skip button
Web client only auto-prompts for Intro/Outro by default. Filler segments use `MediaSegmentType.Unknown`. Users must set `segmentTypeAction__Unknown` to `AskToSkip` or `Skip` via DisplayPreferences API or settings.

## Slug generation
`SlugHelper.GenerateSlug`: lowercase, regex strip `[^a-z0-9\s-]`, spaces→hyphens, collapse consecutive hyphens, trim hyphens from ends.
