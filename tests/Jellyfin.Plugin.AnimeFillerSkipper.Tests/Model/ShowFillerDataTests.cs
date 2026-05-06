using System.Collections.Generic;
using Jellyfin.Plugin.AnimeFillerSkipper.Model;
using Xunit;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Tests;

public class ShowFillerDataTests
{
    [Fact]
    public void IsFiller_PureFillerEpisode_ReturnsTrue()
    {
        var data = new ShowFillerData
        {
            FillerEpisodes = new HashSet<int> { 33, 50, 64 },
            MixedEpisodes = new HashSet<int>()
        };

        Assert.True(data.IsFiller(33, true));
        Assert.True(data.IsFiller(33, false));
    }

    [Fact]
    public void IsFiller_MixedEpisode_TreatMixedTrue_ReturnsTrue()
    {
        var data = new ShowFillerData
        {
            FillerEpisodes = new HashSet<int>(),
            MixedEpisodes = new HashSet<int> { 8, 27 }
        };

        Assert.True(data.IsFiller(8, true));
    }

    [Fact]
    public void IsFiller_MixedEpisode_TreatMixedFalse_ReturnsFalse()
    {
        var data = new ShowFillerData
        {
            FillerEpisodes = new HashSet<int>(),
            MixedEpisodes = new HashSet<int> { 8, 27 }
        };

        Assert.False(data.IsFiller(8, false));
    }

    [Fact]
    public void IsFiller_CanonEpisode_ReturnsFalse()
    {
        var data = new ShowFillerData
        {
            FillerEpisodes = new HashSet<int> { 33 },
            MixedEpisodes = new HashSet<int> { 8 }
        };

        Assert.False(data.IsFiller(1, true));
        Assert.False(data.IsFiller(1, false));
    }

    [Fact]
    public void IsFiller_EmptyData_ReturnsFalse()
    {
        var data = new ShowFillerData();

        Assert.False(data.IsFiller(1, true));
        Assert.False(data.IsFiller(1, false));
    }

    [Fact]
    public void IsFiller_NegativeEpisode_ReturnsFalse()
    {
        var data = new ShowFillerData
        {
            FillerEpisodes = new HashSet<int> { 1, 2, 3 }
        };

        Assert.False(data.IsFiller(-1, true));
    }

    [Fact]
    public void IsFiller_ZeroEpisode_ReturnsFalse()
    {
        var data = new ShowFillerData
        {
            FillerEpisodes = new HashSet<int> { 1, 2, 3 }
        };

        Assert.False(data.IsFiller(0, true));
    }
}
