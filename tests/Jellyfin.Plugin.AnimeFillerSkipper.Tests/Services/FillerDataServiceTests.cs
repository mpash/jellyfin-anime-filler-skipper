using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AnimeFillerSkipper.Model;
using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Tests.Services;

public class FillerDataServiceTests : IDisposable
{
    private readonly string _tempPath;

    public FillerDataServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"jellyfin_filler_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    private FillerDataService CreateService()
    {
        var paths = new Mock<IApplicationPaths>();
        paths.Setup(p => p.DataPath).Returns(_tempPath);

        return new FillerDataService(paths.Object, NullLogger<FillerDataService>.Instance);
    }

    [Fact]
    public async Task StoreAndGet_ReturnsStoredData()
    {
        var service = CreateService();
        var slug = "bleach";
        var data = new ShowFillerData
        {
            ShowName = "Bleach",
            FillerEpisodes = new() { 33, 50, 64 },
            MixedEpisodes = new() { 8, 27 }
        };

        await service.StoreFillerDataAsync(slug, data, CancellationToken.None);
        var result = await service.GetFillerDataAsync(slug, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Bleach", result!.ShowName);
        Assert.Contains(33, result.FillerEpisodes);
        Assert.Contains(8, result.MixedEpisodes);
    }

    [Fact]
    public async Task GetFillerData_SlugCaseInsensitive()
    {
        var service = CreateService();
        var data = new ShowFillerData { ShowName = "Bleach" };

        await service.StoreFillerDataAsync("Bleach", data, CancellationToken.None);
        var result = await service.GetFillerDataAsync("BLEACH", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("bleach", result!.Slug);
    }

    [Fact]
    public async Task GetFillerData_NotStored_ReturnsNull()
    {
        var service = CreateService();

        var result = await service.GetFillerDataAsync("unknown", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task MapSeriesToSlug_And_GetFillerDataForSeries_Works()
    {
        var service = CreateService();
        var seriesId = Guid.NewGuid();
        var data = new ShowFillerData
        {
            ShowName = "Naruto",
            FillerEpisodes = new() { 136 }
        };

        await service.StoreFillerDataAsync("naruto", data, CancellationToken.None);
        await service.MapSeriesToSlugAsync(seriesId, "naruto", CancellationToken.None);

        var result = await service.GetFillerDataForSeriesAsync(seriesId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Naruto", result!.ShowName);
        Assert.Contains(136, result.FillerEpisodes);
    }

    [Fact]
    public async Task GetFillerDataForSeries_NoMapping_ReturnsNull()
    {
        var service = CreateService();

        var result = await service.GetFillerDataForSeriesAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public void GetSlugForSeries_NoStoreLoaded_ReturnsNull()
    {
        var service = CreateService();

        var result = service.GetSlugForSeries(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSlugForSeries_AfterMapping_ReturnsSlug()
    {
        var service = CreateService();
        var seriesId = Guid.NewGuid();

        await service.MapSeriesToSlugAsync(seriesId, "bleach", CancellationToken.None);

        var result = service.GetSlugForSeries(seriesId);

        Assert.Equal("bleach", result);
    }

    [Fact]
    public async Task StoreFillerData_UpdatesLastUpdated()
    {
        var service = CreateService();
        var data = new ShowFillerData { ShowName = "Test" };

        await service.StoreFillerDataAsync("test", data, CancellationToken.None);
        var result = await service.GetFillerDataAsync("test", CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.LastUpdated > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task StoreFillerData_OverwritesExisting()
    {
        var service = CreateService();
        var data1 = new ShowFillerData { ShowName = "V1", FillerEpisodes = new() { 1 } };
        var data2 = new ShowFillerData { ShowName = "V2", FillerEpisodes = new() { 2, 3 } };

        await service.StoreFillerDataAsync("test", data1, CancellationToken.None);
        await service.StoreFillerDataAsync("test", data2, CancellationToken.None);

        var result = await service.GetFillerDataAsync("test", CancellationToken.None);

        Assert.Equal("V2", result!.ShowName);
        Assert.Equal(2, result.FillerEpisodes.Count);
    }
}
