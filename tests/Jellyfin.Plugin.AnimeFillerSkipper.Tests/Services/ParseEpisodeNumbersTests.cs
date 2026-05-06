using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using Xunit;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Tests.Services;

public class ParseEpisodeNumbersTests
{
    [Fact]
    public void SingleNumber_ReturnsThatNumber()
    {
        var result = FillerDataParser.ParseEpisodeNumbers("42");

        Assert.Single(result);
        Assert.Contains(42, result);
    }

    [Fact]
    public void Range_ReturnsAllNumbersInRange()
    {
        var result = FillerDataParser.ParseEpisodeNumbers("1-5");

        Assert.Equal(5, result.Count);
        Assert.Contains(1, result);
        Assert.Contains(2, result);
        Assert.Contains(3, result);
        Assert.Contains(4, result);
        Assert.Contains(5, result);
    }

    [Fact]
    public void CommaSeparatedSingles_ReturnsAll()
    {
        var result = FillerDataParser.ParseEpisodeNumbers("33, 50, 64, 108");

        Assert.Equal(4, result.Count);
        Assert.Contains(33, result);
        Assert.Contains(50, result);
        Assert.Contains(64, result);
        Assert.Contains(108, result);
    }

    [Fact]
    public void CommaSeparatedWithRanges_ReturnsAll()
    {
        var result = FillerDataParser.ParseEpisodeNumbers("1-3,5,7-9");

        Assert.Equal(7, result.Count);
        Assert.Contains(1, result);
        Assert.Contains(2, result);
        Assert.Contains(3, result);
        Assert.Contains(5, result);
        Assert.Contains(7, result);
        Assert.Contains(8, result);
        Assert.Contains(9, result);
    }

    [Fact]
    public void EmptyString_ReturnsEmpty()
    {
        var result = FillerDataParser.ParseEpisodeNumbers("");

        Assert.Empty(result);
    }

    [Fact]
    public void WhitespaceOnly_ReturnsEmpty()
    {
        var result = FillerDataParser.ParseEpisodeNumbers("   ");

        Assert.Empty(result);
    }

    [Fact]
    public void InvalidEntries_IgnoresThem()
    {
        var result = FillerDataParser.ParseEpisodeNumbers("1,abc,3,xyz-zy,5");

        Assert.Equal(3, result.Count);
        Assert.Contains(1, result);
        Assert.Contains(3, result);
        Assert.Contains(5, result);
    }

    [Fact]
    public void Duplicates_ReturnsUniqueSet()
    {
        var result = FillerDataParser.ParseEpisodeNumbers("1, 1, 2, 2, 3");

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void LargeRange_Works()
    {
        var result = FillerDataParser.ParseEpisodeNumbers("1-366");

        Assert.Equal(366, result.Count);
    }
}
