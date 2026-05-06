using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Model;
using MediaBrowser.Model.MediaSegments;
using Microsoft.Extensions.Logging;
using Jellyfin.Database.Implementations.Enums;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Providers;

public class FillerSegmentProvider : IMediaSegmentProvider
{
    private readonly ILibraryManager _libraryManager;
    private readonly IFillerDataService _fillerDataService;
    private readonly ILogger<FillerSegmentProvider> _logger;

    public string Name => "AnimeFillerSkipper";

    public FillerSegmentProvider(
        ILibraryManager libraryManager,
        IFillerDataService fillerDataService,
        ILogger<FillerSegmentProvider> logger)
    {
        _libraryManager = libraryManager;
        _fillerDataService = fillerDataService;
        _logger = logger;
    }

    public ValueTask<bool> Supports(BaseItem item)
    {
        if (item is Episode episode
            && episode.SeriesId != Guid.Empty
            && episode.IndexNumber.HasValue
            && episode.IndexNumber.Value > 0)
        {
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    public async Task<IReadOnlyList<MediaSegmentDto>> GetMediaSegments(
        MediaSegmentGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = _libraryManager.GetItemById(request.ItemId);
            if (item is not Episode episode
                || episode.SeriesId == Guid.Empty
                || !episode.IndexNumber.HasValue)
            {
                return Array.Empty<MediaSegmentDto>();
            }

            var showData = await _fillerDataService
                .GetFillerDataForSeriesAsync(episode.SeriesId, cancellationToken)
                .ConfigureAwait(false);

            if (showData == null)
            {
                return Array.Empty<MediaSegmentDto>();
            }

            var episodeNumber = episode.IndexNumber.Value;
            if (!showData.IsFiller(episodeNumber, true))
            {
                return Array.Empty<MediaSegmentDto>();
            }

            var runtimeTicks = episode.RunTimeTicks ?? 0;
            if (runtimeTicks <= 0)
            {
                _logger.LogDebug(
                    "Episode {Episode} has no runtime ticks, skipping segment creation",
                    episodeNumber);
                return Array.Empty<MediaSegmentDto>();
            }

            var segment = new MediaSegmentDto
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                Type = MediaSegmentType.Unknown,
                StartTicks = 0,
                EndTicks = runtimeTicks
            };

            _logger.LogInformation(
                "Created filler segment for {Show} episode {Episode}",
                episode.SeriesName, episodeNumber);

            return new List<MediaSegmentDto> { segment };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating filler segments");
            return Array.Empty<MediaSegmentDto>();
        }
    }

    public Task CleanupExtractedData(Guid itemId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
