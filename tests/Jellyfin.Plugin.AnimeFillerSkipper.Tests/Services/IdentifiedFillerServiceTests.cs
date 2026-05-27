using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AnimeFillerSkipper.Model;
using Jellyfin.Plugin.AnimeFillerSkipper.Models;
using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Tests.Services;

public class IdentifiedFillerServiceTests
{
    [Fact]
    public async Task GetIdentifiedFillerAsync_UsesLibraryAndCacheData()
    {
        var series = new Series
        {
            Id = Guid.NewGuid(),
            Name = "Bleach"
        };
        var episode = new Episode
        {
            Id = Guid.NewGuid(),
            SeriesId = series.Id,
            Name = "Filler Episode",
            ParentIndexNumber = 1,
            IndexNumber = 2
        };
        var data = new ShowFillerData
        {
            Slug = "bleach",
            FillerEpisodes = new HashSet<int> { 2 }
        };
        var libraryManager = new Mock<ILibraryManager>();
        var fillerDataService = new Mock<IFillerDataService>();

        libraryManager
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(query => query.IncludeItemTypes.Contains(BaseItemKind.Series))))
            .Returns(new List<BaseItem> { series });
        libraryManager
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(query => query.IncludeItemTypes.Contains(BaseItemKind.Episode))))
            .Returns(new List<BaseItem> { episode });
        fillerDataService
            .Setup(s => s.GetFillerDataForSeriesAsync(series.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);
        fillerDataService
            .Setup(s => s.GetSlugForSeries(series.Id))
            .Returns("bleach");

        var service = new IdentifiedFillerService(libraryManager.Object, fillerDataService.Object);

        var result = await service.GetIdentifiedFillerAsync(CancellationToken.None);

        var resultSeries = Assert.Single(result.Series);
        Assert.Equal("Bleach", resultSeries.Name);
        Assert.Equal("bleach", resultSeries.Slug);
        Assert.Equal(1, resultSeries.FillerCount);
        Assert.Equal(IdentifiedFillerStates.Filler, resultSeries.Seasons[0].Episodes[0].FillerState);
    }

    [Fact]
    public void GetFillerState_NoCache_ReturnsNone()
    {
        Assert.Equal(
            IdentifiedFillerStates.None,
            IdentifiedFillerService.GetFillerState(1, null));
    }

    [Fact]
    public void GetFillerState_FillerAndMixedEpisodesStaySeparate()
    {
        var data = new ShowFillerData
        {
            FillerEpisodes = new HashSet<int> { 2 },
            MixedEpisodes = new HashSet<int> { 3 }
        };

        Assert.Equal(IdentifiedFillerStates.Filler, IdentifiedFillerService.GetFillerState(2, data));
        Assert.Equal(IdentifiedFillerStates.Mixed, IdentifiedFillerService.GetFillerState(3, data));
        Assert.Equal(IdentifiedFillerStates.None, IdentifiedFillerService.GetFillerState(4, data));
    }

    [Fact]
    public void GetFillerState_EpisodeWithoutIndex_ReturnsNone()
    {
        var data = new ShowFillerData
        {
            FillerEpisodes = new HashSet<int> { 1 }
        };

        Assert.Equal(
            IdentifiedFillerStates.None,
            IdentifiedFillerService.GetFillerState(null, data));
    }

    [Fact]
    public void BuildSeriesDto_GroupsEpisodesBySeasonAndCountsStates()
    {
        var series = new Series
        {
            Id = Guid.NewGuid(),
            Name = "Test Show"
        };
        var data = new ShowFillerData
        {
            LastUpdated = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            FillerEpisodes = new HashSet<int> { 2 },
            MixedEpisodes = new HashSet<int> { 3 }
        };
        var episodes = new[]
        {
            new Episode { Id = Guid.NewGuid(), Name = "Canon", ParentIndexNumber = 1, IndexNumber = 1 },
            new Episode { Id = Guid.NewGuid(), Name = "Filler", ParentIndexNumber = 1, IndexNumber = 2 },
            new Episode { Id = Guid.NewGuid(), Name = "Mixed", ParentIndexNumber = 2, IndexNumber = 3 },
            new Episode { Id = Guid.NewGuid(), Name = "Unknown", ParentIndexNumber = 2 }
        };

        var result = IdentifiedFillerService.BuildSeriesDto(series, episodes, data, "test-show");

        Assert.Equal("Test Show", result.Name);
        Assert.Equal("test-show", result.Slug);
        Assert.True(result.HasCachedData);
        Assert.Equal(1, result.FillerCount);
        Assert.Equal(1, result.MixedCount);
        Assert.Equal(2, result.Seasons.Count);
        Assert.Equal("Season 1", result.Seasons[0].Name);
        Assert.Equal("Season 2", result.Seasons[1].Name);
        Assert.Equal(IdentifiedFillerStates.Filler, result.Seasons[0].Episodes[1].FillerState);
        Assert.Equal(IdentifiedFillerStates.Mixed, result.Seasons[1].Episodes[0].FillerState);
        Assert.Equal(IdentifiedFillerStates.None, result.Seasons[1].Episodes[1].FillerState);
    }
}
