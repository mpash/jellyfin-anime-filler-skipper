using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AnimeFillerSkipper.Model;
using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Tests.Services;

public class AnimeFillerListClientTests
{
    [Fact]
    public async Task FetchShowPageAsync_Success_ReturnsHtml()
    {
        var expectedHtml = "<html><body>data</body></html>";
        var handler = CreateHandler(expectedHtml, HttpStatusCode.OK);

        var httpClient = new HttpClient(handler.Object) { Timeout = TimeSpan.FromSeconds(5) };
        var client = new TestableAnimeFillerListClient(httpClient, NullLogger<AnimeFillerListClient>.Instance);

        var result = await client.FetchShowPageAsync("bleach", CancellationToken.None);

        Assert.Equal(expectedHtml, result);
    }

    [Fact]
    public async Task FetchShowPageWithEtagAsync_Success_ReturnsHtmlAndEtag()
    {
        var expectedHtml = "<html><body>data</body></html>";
        var etag = "\"abc123\"";
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedHtml),
                Headers = { ETag = EntityTagHeaderValue.Parse(etag) }
            });

        var httpClient = new HttpClient(handler.Object) { Timeout = TimeSpan.FromSeconds(5) };
        var client = new TestableAnimeFillerListClient(httpClient, NullLogger<AnimeFillerListClient>.Instance);

        var response = await client.FetchShowPageWithEtagAsync("bleach", null, CancellationToken.None);

        Assert.Equal(expectedHtml, response.Html);
        Assert.Equal(etag, response.Etag);
        Assert.False(response.NotModified);
    }

    [Fact]
    public async Task FetchShowPageWithEtagAsync_NotModified_ReturnsNotModified()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotModified));

        var httpClient = new HttpClient(handler.Object) { Timeout = TimeSpan.FromSeconds(5) };
        var client = new TestableAnimeFillerListClient(httpClient, NullLogger<AnimeFillerListClient>.Instance);

        var response = await client.FetchShowPageWithEtagAsync("bleach", "\"etag\"", CancellationToken.None);

        Assert.True(response.NotModified);
        Assert.Null(response.Html);
    }

    [Fact]
    public async Task FetchShowPageWithEtagAsync_SendsIfNoneMatch()
    {
        var etag = "\"abc123\"";
        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(
                (req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("html")
            });

        var httpClient = new HttpClient(handler.Object) { Timeout = TimeSpan.FromSeconds(5) };
        var client = new TestableAnimeFillerListClient(httpClient, NullLogger<AnimeFillerListClient>.Instance);

        await client.FetchShowPageWithEtagAsync("bleach", etag, CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest!.Headers.IfNoneMatch);
        Assert.Contains(etag, capturedRequest.Headers.IfNoneMatch.ToString());
    }

    [Fact]
    public async Task FetchShowPageWithEtagAsync_NullEtag_DoesNotSendIfNoneMatch()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(
                (req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("html")
            });

        var httpClient = new HttpClient(handler.Object) { Timeout = TimeSpan.FromSeconds(5) };
        var client = new TestableAnimeFillerListClient(httpClient, NullLogger<AnimeFillerListClient>.Instance);

        await client.FetchShowPageWithEtagAsync("bleach", null, CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Empty(capturedRequest!.Headers.IfNoneMatch);
    }

    [Fact]
    public async Task FetchShowPageWithEtagAsync_404_ReturnsNullHtml()
    {
        var handler = CreateHandler("", HttpStatusCode.NotFound);

        var httpClient = new HttpClient(handler.Object) { Timeout = TimeSpan.FromSeconds(5) };
        var client = new TestableAnimeFillerListClient(httpClient, NullLogger<AnimeFillerListClient>.Instance);

        var response = await client.FetchShowPageWithEtagAsync("nonexistent", null, CancellationToken.None);

        Assert.Null(response.Html);
        Assert.Null(response.Etag);
        Assert.False(response.NotModified);
    }

    [Fact]
    public async Task FetchShowPageWithEtagAsync_NetworkError_ReturnsEmpty()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handler.Object) { Timeout = TimeSpan.FromSeconds(5) };
        var client = new TestableAnimeFillerListClient(httpClient, NullLogger<AnimeFillerListClient>.Instance);

        var response = await client.FetchShowPageWithEtagAsync("bleach", null, CancellationToken.None);

        Assert.Null(response.Html);
        Assert.Null(response.Etag);
        Assert.False(response.NotModified);
    }

    [Fact]
    public async Task FetchShowPageAsync_TrimsSlug()
    {
        var handler = new Mock<HttpMessageHandler>();
        HttpRequestMessage? capturedRequest = null;
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(
                (req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("html")
            });

        var httpClient = new HttpClient(handler.Object) { Timeout = TimeSpan.FromSeconds(5) };
        var client = new TestableAnimeFillerListClient(httpClient, NullLogger<AnimeFillerListClient>.Instance);

        await client.FetchShowPageAsync("  Bleach  ", CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.EndsWith("bleach", capturedRequest!.RequestUri!.AbsolutePath);
    }

    private static Mock<HttpMessageHandler> CreateHandler(string content, HttpStatusCode statusCode)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
        return handler;
    }

    private class TestableAnimeFillerListClient : AnimeFillerListClient
    {
        public TestableAnimeFillerListClient(HttpClient httpClient, Microsoft.Extensions.Logging.ILogger<AnimeFillerListClient> logger)
            : base(logger)
        {
            var field = typeof(AnimeFillerListClient).GetField("_httpClient",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(this, httpClient);
        }
    }
}
