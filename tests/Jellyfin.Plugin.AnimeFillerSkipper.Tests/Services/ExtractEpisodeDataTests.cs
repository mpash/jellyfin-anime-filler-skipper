using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using Xunit;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Tests.Services;

public class ExtractEpisodeDataTests
{
    [Fact]
    public void DataEndingWithCategory_StripsTrailingCategory()
    {
        var text = "1-7, 9-26, 28-31Mixed Canon/Filler Episodes:";

        var result = FillerDataParser.ExtractEpisodeData(text);

        Assert.Equal("1-7, 9-26, 28-31", result);
    }

    [Fact]
    public void DataEndingWithFillerCategory_StripsIt()
    {
        var text = "33, 50, 64-108Filler Episodes:";

        var result = FillerDataParser.ExtractEpisodeData(text);

        Assert.Equal("33, 50, 64-108", result);
    }

    [Fact]
    public void CleanData_ReturnsAsIs()
    {
        var text = "1, 2, 3, 4, 5";

        var result = FillerDataParser.ExtractEpisodeData(text);

        Assert.Equal("1, 2, 3, 4, 5", result);
    }

    [Fact]
    public void EmptyString_ReturnsEmpty()
    {
        var result = FillerDataParser.ExtractEpisodeData("");

        Assert.Equal("", result);
    }
}
