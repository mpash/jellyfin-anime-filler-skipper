using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AnimeFillerSkipper.Model;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Services;

public interface IAnimeFillerListClient
{
    Task<string?> FetchShowPageAsync(string slug, CancellationToken cancellationToken);
    Task<FillerListResponse> FetchShowPageWithEtagAsync(string slug, string? etag, CancellationToken cancellationToken);
}

public class AnimeFillerListClient : IAnimeFillerListClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnimeFillerListClient> _logger;
    private const string BaseUrl = "https://www.animefillerlist.com/shows/";

    public AnimeFillerListClient(ILogger<AnimeFillerListClient> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Jellyfin-AnimeFillerSkipper/1.0 (+https://github.com)");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<string?> FetchShowPageAsync(string slug, CancellationToken cancellationToken)
    {
        var response = await FetchShowPageWithEtagAsync(slug, null, cancellationToken).ConfigureAwait(false);
        return response.Html;
    }

    public async Task<FillerListResponse> FetchShowPageWithEtagAsync(
        string slug, string? etag, CancellationToken cancellationToken)
    {
        var url = BaseUrl + slug.Trim().ToLowerInvariant();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (!string.IsNullOrEmpty(etag))
            {
                request.Headers.IfNoneMatch.ParseAdd(etag);
            }

            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                _logger.LogDebug("Not modified: {Url}", url);
                return new FillerListResponse { NotModified = true };
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch {Url}: {StatusCode}", url, response.StatusCode);
                return new FillerListResponse();
            }

            var html = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            var newEtag = response.Headers.ETag?.Tag;
            _logger.LogDebug("Fetched {Url} (etag: {Etag})", url, newEtag);

            return new FillerListResponse { Html = html, Etag = newEtag };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error fetching {Url}", url);
            return new FillerListResponse();
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Request timed out for {Url}", url);
            return new FillerListResponse();
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
