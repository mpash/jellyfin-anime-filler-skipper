using System.Collections.Generic;
using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using Xunit;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Tests.Services;

public class ParseFromQuickListTests
{
    [Fact]
    public void QuickList_ExtractsFillerEpisodes()
    {
        var text = "Filler Episodes:33, 50, 64-108, 128-137"
            + "Mixed Canon/Filler Episodes:8, 27, 32Manga Canon Episodes:1-7";

        var result = FillerDataParser.ParseFromQuickList(text, "Filler Episodes:");

        Assert.Contains(33, result);
        Assert.Contains(50, result);
        Assert.Contains(64, result);
        Assert.Contains(100, result);
        Assert.Contains(128, result);
    }

    [Fact]
    public void QuickList_StopsAtNextCategory()
    {
        var text = "Filler Episodes:5,10,15Mixed Canon/Filler Episodes:20Manga Canon Episodes:1-4";

        var result = FillerDataParser.ParseFromQuickList(text, "Filler Episodes:");

        Assert.Equal(3, result.Count);
        Assert.Contains(5, result);
        Assert.Contains(10, result);
        Assert.Contains(15, result);
        Assert.DoesNotContain(20, result);
    }

    [Fact]
    public void QuickList_PrefixNotFound_ReturnsEmpty()
    {
        var text = "Some other text";

        var result = FillerDataParser.ParseFromQuickList(text, "Filler Episodes:");

        Assert.Empty(result);
    }

    [Fact]
    public void QuickList_LastCategory_ReadsToEnd()
    {
        var text = "Manga Canon Episodes:1-4Mixed Canon/Filler Episodes:5-6Filler Episodes:10,20,30";

        var result = FillerDataParser.ParseFromQuickList(text, "Filler Episodes:");

        Assert.Equal(3, result.Count);
        Assert.Contains(10, result);
        Assert.Contains(20, result);
        Assert.Contains(30, result);
    }
}
