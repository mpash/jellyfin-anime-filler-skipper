using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AnimeFillerSkipper.Configuration;
using Jellyfin.Plugin.AnimeFillerSkipper.Model;
using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using Jellyfin.Plugin.AnimeFillerSkipper.Utilities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AnimeFillerSkipper.ScheduledTasks;

public class UpdateFillerDataTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IFillerDataService _fillerDataService;
    private readonly IAnimeFillerListClient _fillerListClient;
    private readonly ILogger<UpdateFillerDataTask> _logger;
    private readonly PluginConfiguration _config;

    public string Name => "Update Anime Filler Data";
    public string Key => "AnimeFillerSkipperUpdate";
    public string Description => "Scans anime libraries and fetches filler episode data from animefillerlist.com. Uses ETag-based cache validation to avoid unnecessary downloads.";
    public string Category => "Anime Filler Skipper";

    public UpdateFillerDataTask(
        ILibraryManager libraryManager,
        IFillerDataService fillerDataService,
        IAnimeFillerListClient fillerListClient,
        ILogger<UpdateFillerDataTask> logger,
        Plugin plugin)
    {
        _libraryManager = libraryManager;
        _fillerDataService = fillerDataService;
        _fillerListClient = fillerListClient;
        _logger = logger;
        _config = plugin.Configuration;
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(4).Ticks,
                MaxRuntimeTicks = TimeSpan.FromHours(1).Ticks
            }
        };
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var cacheExpiration = TimeSpan.FromHours(_config.CacheExpirationHours);
        var requestDelay = TimeSpan.FromMilliseconds(_config.RequestDelayMs);

        _logger.LogInformation(
            "Starting anime filler data update (cache: {CacheHours}h, delay: {DelayMs}ms)",
            _config.CacheExpirationHours, _config.RequestDelayMs);

        var seriesList = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Series },
            IsVirtualItem = false,
            Recursive = true
        })
        .OfType<Series>()
        .ToList();

        _logger.LogInformation("Found {Count} series in libraries", seriesList.Count);

        var processedCount = 0;
        var skippedCount = 0;
        var fetchedCount = 0;
        var errorCount = 0;
        var totalCount = seriesList.Count;

        foreach (var series in seriesList)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var slug = SlugHelper.GenerateSlug(series.Name);
                if (string.IsNullOrEmpty(slug))
                {
                    processedCount++;
                    continue;
                }

                await _fillerDataService
                    .MapSeriesToSlugAsync(series.Id, slug, cancellationToken)
                    .ConfigureAwait(false);

                var existingData = await _fillerDataService
                    .GetFillerDataAsync(slug, cancellationToken)
                    .ConfigureAwait(false);

                var age = existingData != null
                    ? DateTime.UtcNow - existingData.LastUpdated
                    : TimeSpan.MaxValue;

                if (existingData != null && age < cacheExpiration)
                {
                    _logger.LogTrace("Cache valid for {Slug} (age: {Age}h)", slug, age.TotalHours.ToString("F1"));
                    skippedCount++;
                    processedCount++;
                    progress.Report((double)processedCount / totalCount * 100);
                    continue;
                }

                var cachedEtag = existingData?.Etag;

                var response = await _fillerListClient
                    .FetchShowPageWithEtagAsync(slug, cachedEtag, cancellationToken)
                    .ConfigureAwait(false);

                if (response.NotModified)
                {
                    if (existingData != null)
                    {
                        existingData.LastUpdated = DateTime.UtcNow;
                        await _fillerDataService
                            .StoreFillerDataAsync(slug, existingData, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    _logger.LogDebug("ETag match for {Slug}, kept cached data", slug);
                    skippedCount++;
                    processedCount++;
                    progress.Report((double)processedCount / totalCount * 100);
                    await Task.Delay(requestDelay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (response.Html == null)
                {
                    if (existingData != null)
                    {
                        _logger.LogDebug(
                            "Fetch failed for {Slug}, keeping stale cache (age: {Age}h)",
                            slug, age.TotalHours.ToString("F1"));
                    }
                    else
                    {
                        _logger.LogWarning("No cached data and fetch failed for {Slug}", slug);
                        errorCount++;
                    }

                    processedCount++;
                    progress.Report((double)processedCount / totalCount * 100);
                    await Task.Delay(requestDelay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var (filler, mixed, showName) = FillerDataParser.Parse(response.Html, _logger);

                if (filler.Count == 0 && mixed.Count == 0 && existingData != null)
                {
                    _logger.LogWarning(
                        "Parse produced no episodes for {Slug}, keeping cached data", slug);
                    processedCount++;
                    progress.Report((double)processedCount / totalCount * 100);
                    await Task.Delay(requestDelay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var showData = new ShowFillerData
                {
                    ShowName = showName ?? series.Name,
                    FillerEpisodes = filler,
                    MixedEpisodes = mixed,
                    LastUpdated = DateTime.UtcNow,
                    Etag = response.Etag,
                    ItemIds = new HashSet<Guid> { series.Id }
                };

                await _fillerDataService
                    .StoreFillerDataAsync(slug, showData, cancellationToken)
                    .ConfigureAwait(false);

                fetchedCount++;
                processedCount++;
                progress.Report((double)processedCount / totalCount * 100);

                await Task.Delay(requestDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing series {SeriesName}", series.Name);
                errorCount++;
                processedCount++;
            }
        }

        _logger.LogInformation(
            "Anime filler data update complete. Total: {Total}, " +
            "Fetched: {Fetched}, Cache hits: {Skipped}, Errors: {Errors}",
            processedCount, fetchedCount, skippedCount, errorCount);
    }
}
