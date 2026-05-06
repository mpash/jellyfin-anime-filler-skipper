using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Services;

public interface IFillerDataService
{
    Task<Model.ShowFillerData?> GetFillerDataAsync(string slug, CancellationToken cancellationToken);
    Task<Model.ShowFillerData?> GetFillerDataForSeriesAsync(Guid seriesId, CancellationToken cancellationToken);
    Task StoreFillerDataAsync(string slug, Model.ShowFillerData data, CancellationToken cancellationToken);
    Task MapSeriesToSlugAsync(Guid seriesId, string slug, CancellationToken cancellationToken);
    string? GetSlugForSeries(Guid seriesId);
}

public class FillerDataService : IFillerDataService
{
    private readonly string _dataPath;
    private readonly ILogger<FillerDataService> _logger;
    private Model.FillerDataStore? _store;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public FillerDataService(
        IApplicationPaths applicationPaths,
        ILogger<FillerDataService> logger)
    {
        _logger = logger;
        _dataPath = Path.Combine(applicationPaths.DataPath, "anime_filler_data.json");
    }

    private async Task<Model.FillerDataStore> GetStoreAsync()
    {
        if (_store != null) return _store;

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_store != null) return _store;

            if (File.Exists(_dataPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_dataPath).ConfigureAwait(false);
                    _store = JsonSerializer.Deserialize<Model.FillerDataStore>(json) ?? new Model.FillerDataStore();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load filler data store, creating new");
                    _store = new Model.FillerDataStore();
                }
            }
            else
            {
                _store = new Model.FillerDataStore();
            }
        }
        finally
        {
            _lock.Release();
        }

        return _store;
    }

    private async Task SaveStoreAsync()
    {
        var store = _store;
        if (store == null) return;

        try
        {
            var json = JsonSerializer.Serialize(store, _jsonOptions);
            await File.WriteAllTextAsync(_dataPath, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save filler data store");
        }
    }

    public async Task<Model.ShowFillerData?> GetFillerDataAsync(string slug, CancellationToken cancellationToken)
    {
        var store = await GetStoreAsync().ConfigureAwait(false);
        store.Shows.TryGetValue(slug.ToLowerInvariant(), out var data);
        return data;
    }

    public async Task<Model.ShowFillerData?> GetFillerDataForSeriesAsync(Guid seriesId, CancellationToken cancellationToken)
    {
        var store = await GetStoreAsync().ConfigureAwait(false);
        if (store.ItemToSlug.TryGetValue(seriesId, out var slug))
        {
            return await GetFillerDataAsync(slug, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    public async Task StoreFillerDataAsync(string slug, Model.ShowFillerData data, CancellationToken cancellationToken)
    {
        var store = await GetStoreAsync().ConfigureAwait(false);
        var key = slug.ToLowerInvariant();
        data.Slug = key;
        data.LastUpdated = DateTime.UtcNow;
        store.Shows[key] = data;
        await SaveStoreAsync().ConfigureAwait(false);
    }

    public async Task MapSeriesToSlugAsync(Guid seriesId, string slug, CancellationToken cancellationToken)
    {
        var store = await GetStoreAsync().ConfigureAwait(false);
        store.ItemToSlug[seriesId] = slug.ToLowerInvariant();
        await SaveStoreAsync().ConfigureAwait(false);
    }

    public string? GetSlugForSeries(Guid seriesId)
    {
        var store = _store;
        if (store == null) return null;
        store.ItemToSlug.TryGetValue(seriesId, out var slug);
        return slug;
    }
}
