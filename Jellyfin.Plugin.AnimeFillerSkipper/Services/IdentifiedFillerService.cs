using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AnimeFillerSkipper.Model;
using Jellyfin.Plugin.AnimeFillerSkipper.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Services;

public interface IIdentifiedFillerService
{
    Task<IdentifiedFillerResultDto> GetIdentifiedFillerAsync(CancellationToken cancellationToken);
}

public class IdentifiedFillerService : IIdentifiedFillerService
{
    private readonly ILibraryManager _libraryManager;
    private readonly IFillerDataService _fillerDataService;

    public IdentifiedFillerService(
        ILibraryManager libraryManager,
        IFillerDataService fillerDataService)
    {
        _libraryManager = libraryManager;
        _fillerDataService = fillerDataService;
    }

    public async Task<IdentifiedFillerResultDto> GetIdentifiedFillerAsync(CancellationToken cancellationToken)
    {
        var seriesList = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Series },
            IsVirtualItem = false,
            Recursive = true
        })
        .OfType<Series>()
        .OrderBy(series => series.SortName ?? series.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

        var episodesBySeries = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Episode },
            IsVirtualItem = false,
            Recursive = true
        })
        .OfType<Episode>()
        .GroupBy(episode => episode.SeriesId)
        .ToDictionary(group => group.Key, group => group.ToList());

        var seriesDtos = new List<IdentifiedFillerSeriesDto>(seriesList.Count);

        foreach (var series in seriesList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var showData = await _fillerDataService
                .GetFillerDataForSeriesAsync(series.Id, cancellationToken)
                .ConfigureAwait(false);

            var slug = _fillerDataService.GetSlugForSeries(series.Id) ?? showData?.Slug;
            episodesBySeries.TryGetValue(series.Id, out var episodes);

            seriesDtos.Add(BuildSeriesDto(series, episodes ?? new List<Episode>(), showData, slug));
        }

        return new IdentifiedFillerResultDto { Series = seriesDtos };
    }

    internal static IdentifiedFillerSeriesDto BuildSeriesDto(
        Series series,
        IReadOnlyCollection<Episode> episodes,
        ShowFillerData? showData,
        string? slug)
    {
        var episodeDtos = episodes
            .OrderBy(episode => episode.ParentIndexNumber ?? int.MaxValue)
            .ThenBy(episode => episode.IndexNumber ?? int.MaxValue)
            .ThenBy(episode => episode.SortName ?? episode.Name, StringComparer.OrdinalIgnoreCase)
            .Select(episode => BuildEpisodeDto(episode, showData))
            .ToList();

        var seasonDtos = episodeDtos
            .GroupBy(episode => new
            {
                episode.SeasonNumber,
                Name = GetSeasonName(episode.SeasonNumber)
            })
            .OrderBy(group => group.Key.SeasonNumber ?? int.MaxValue)
            .ThenBy(group => group.Key.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new IdentifiedFillerSeasonDto
            {
                IndexNumber = group.Key.SeasonNumber,
                Name = group.Key.Name,
                Episodes = group.ToList()
            })
            .ToList();

        return new IdentifiedFillerSeriesDto
        {
            Id = series.Id,
            Name = series.Name,
            Slug = slug,
            HasCachedData = showData != null,
            LastUpdated = showData?.LastUpdated,
            FillerCount = episodeDtos.Count(episode => episode.FillerState == IdentifiedFillerStates.Filler),
            MixedCount = episodeDtos.Count(episode => episode.FillerState == IdentifiedFillerStates.Mixed),
            Seasons = seasonDtos
        };
    }

    internal static IdentifiedFillerEpisodeDto BuildEpisodeDto(Episode episode, ShowFillerData? showData)
    {
        return new IdentifiedFillerEpisodeDto
        {
            Id = episode.Id,
            SeasonNumber = episode.ParentIndexNumber,
            EpisodeNumber = episode.IndexNumber,
            Name = episode.Name,
            RunTimeTicks = episode.RunTimeTicks,
            HasPrimaryImage = !string.IsNullOrEmpty(episode.PrimaryImagePath),
            FillerState = GetFillerState(episode.IndexNumber, showData)
        };
    }

    internal static string GetFillerState(int? episodeNumber, ShowFillerData? showData)
    {
        if (!episodeNumber.HasValue || showData == null)
        {
            return IdentifiedFillerStates.None;
        }

        if (showData.FillerEpisodes.Contains(episodeNumber.Value))
        {
            return IdentifiedFillerStates.Filler;
        }

        return showData.MixedEpisodes.Contains(episodeNumber.Value)
            ? IdentifiedFillerStates.Mixed
            : IdentifiedFillerStates.None;
    }

    private static string GetSeasonName(int? seasonNumber)
    {
        return seasonNumber.HasValue
            ? "Season " + seasonNumber.Value
            : "Season Unknown";
    }
}
