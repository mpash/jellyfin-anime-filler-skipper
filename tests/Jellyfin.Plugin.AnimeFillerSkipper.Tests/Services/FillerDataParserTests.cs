using System.Linq;
using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Tests.Services;

public class FillerDataParserTests
{
    private static readonly string BleachHtml = @"
<html>
<head><title>Bleach Filler List | The Ultimate Anime Filler Guide</title></head>
<body>
    <div>
        Manga Canon Episodes:1-7, 9-26, 28-31, 34-45, 47-49, 51-63
        Mixed Canon/Filler Episodes:8, 27, 32, 46
        Filler Episodes:33, 50, 64-108, 128-137
    </div>
</body>
</html>";

    private static readonly string QuickListHtml = @"
<html>
<head><title>Naruto Filler List | The Ultimate Anime Filler Guide</title></head>
<body>
    Manga Canon Episodes:1-25, 27-96, 100-101, 107-135Mixed Canon/Filler Episodes:26, 97, 102-106Filler Episodes:136-220
</body>
</html>";

    [Fact]
    public void Parse_BleachHtml_ExtractsAllCategories()
    {
        var (filler, mixed, showName) = FillerDataParser.Parse(BleachHtml, NullLogger.Instance);

        Assert.Equal("Bleach", showName);
        Assert.Contains(33, filler);
        Assert.Contains(50, filler);
        Assert.Contains(64, filler);
        Assert.Contains(100, filler);
        Assert.Contains(108, filler);
        Assert.Contains(128, filler);
        Assert.Contains(8, mixed);
        Assert.Contains(27, mixed);
        Assert.Contains(32, mixed);
        Assert.Contains(46, mixed);
    }

    [Fact]
    public void Parse_QuickListConcatenatedFormat_ExtractsAllCategories()
    {
        var (filler, mixed, showName) = FillerDataParser.Parse(QuickListHtml, NullLogger.Instance);

        Assert.Equal("Naruto", showName);
        Assert.Contains(136, filler);
        Assert.Contains(200, filler);
        Assert.Contains(220, filler);
        Assert.Contains(26, mixed);
        Assert.Contains(97, mixed);
        Assert.Contains(102, mixed);
    }

    [Fact]
    public void Parse_QuickListFormat_FallBackWorks()
    {
        var html = @"<html><head><title>Show Filler List | The Ultimate Anime Filler Guide</title></head>
        <body>
        Filler Episodes:10,20,30,40
        </body></html>";

        var (filler, _, _) = FillerDataParser.Parse(html, NullLogger.Instance);

        Assert.Equal(4, filler.Count);
    }

    [Fact]
    public void Parse_NoTitle_ReturnsNullShowName()
    {
        var html = @"<html><body>
        Manga Canon Episodes:1-5Mixed Canon/Filler Episodes:6Filler Episodes:10
        </body></html>";

        var (_, _, showName) = FillerDataParser.Parse(html, NullLogger.Instance);

        Assert.Null(showName);
    }

    [Fact]
    public void Parse_MalformedHtml_ReturnsEmpty()
    {
        var (filler, mixed, _) = FillerDataParser.Parse("not valid html", NullLogger.Instance);

        Assert.Empty(filler);
        Assert.Empty(mixed);
    }

    [Fact]
    public void Parse_EmptyHtml_ReturnsEmpty()
    {
        var (filler, mixed, _) = FillerDataParser.Parse("", NullLogger.Instance);

        Assert.Empty(filler);
        Assert.Empty(mixed);
    }

    [Fact]
    public void Parse_OnlyCanonEpisodes_ReturnsEmptyFillerAndMixed()
    {
        var html = @"<html><head><title>Show Filler List | The Ultimate Anime Filler Guide</title></head>
        <body>Manga Canon Episodes:1-100</body></html>";

        var (filler, mixed, _) = FillerDataParser.Parse(html, NullLogger.Instance);

        Assert.Empty(filler);
        Assert.Empty(mixed);
    }

    [Fact]
    public void Parse_BleachStyle_DoesNotCaptureCanonAsFiller()
    {
        var (filler, mixed, _) = FillerDataParser.Parse(BleachHtml, NullLogger.Instance);

        Assert.DoesNotContain(1, filler);
        Assert.DoesNotContain(1, mixed);
        Assert.DoesNotContain(2, filler);
        Assert.DoesNotContain(2, mixed);
    }

    [Fact]
    public void Parse_LargeEpisodeCounts_Works()
    {
        var html = @"<html><head><title>Show Filler List | The Ultimate Anime Filler Guide</title></head>
        <body>
        Manga Canon Episodes:1-500
        Filler Episodes:501-800
        Mixed Canon/Filler Episodes:801-900
        </body></html>";

        var (filler, mixed, _) = FillerDataParser.Parse(html, NullLogger.Instance);

        Assert.Equal(300, filler.Count);
        Assert.Equal(100, mixed.Count);
    }
}
