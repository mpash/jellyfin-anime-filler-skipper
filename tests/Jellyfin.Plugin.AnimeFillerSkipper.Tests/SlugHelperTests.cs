using Jellyfin.Plugin.AnimeFillerSkipper.Utilities;
using Xunit;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Tests;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Bleach", "bleach")]
    [InlineData("Naruto Shippuden", "naruto-shippuden")]
    [InlineData("Dragon Ball Z", "dragon-ball-z")]
    [InlineData("Attack on Titan", "attack-on-titan")]
    [InlineData("My Hero Academia", "my-hero-academia")]
    [InlineData("One Piece", "one-piece")]
    [InlineData("Fullmetal Alchemist: Brotherhood", "fullmetal-alchemist-brotherhood")]
    [InlineData("Kaguya-sama: Love Is War", "kaguya-sama-love-is-war")]
    [InlineData("Mob Psycho 100", "mob-psycho-100")]
    [InlineData("Fate/Stay Night", "fatestay-night")]
    public void GenerateSlug_ProducesExpectedSlug(string seriesName, string expected)
    {
        var result = SlugHelper.GenerateSlug(seriesName);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("  ")]
    public void GenerateSlug_EmptyOrWhitespace_ReturnsEmpty(string? seriesName)
    {
        var result = SlugHelper.GenerateSlug(seriesName!);
        Assert.Equal("", result);
    }

    [Fact]
    public void GenerateSlug_RemovesSpecialCharacters()
    {
        var result = SlugHelper.GenerateSlug("Steins;Gate");
        Assert.DoesNotContain(";", result);
        Assert.Equal("steinsgate", result);
    }

    [Fact]
    public void GenerateSlug_ReplacesDoubleHyphens()
    {
        var result = SlugHelper.GenerateSlug("Test - Show");
        Assert.Equal("test-show", result);
    }

    [Fact]
    public void GenerateSlug_TrimsWhitespace()
    {
        var result = SlugHelper.GenerateSlug("  Naruto  ");
        Assert.Equal("naruto", result);
    }
}
