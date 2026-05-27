using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AnimeFillerSkipper.Controllers;
using Jellyfin.Plugin.AnimeFillerSkipper.Models;
using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Tests.Controllers;

public class IdentifiedFillerControllerTests
{
    [Fact]
    public async Task GetIdentifiedFiller_ReturnsServiceResult()
    {
        var expected = new IdentifiedFillerResultDto
        {
            Series = new[] { new IdentifiedFillerSeriesDto { Name = "Bleach" } }
        };
        var service = new Mock<IIdentifiedFillerService>();
        service
            .Setup(s => s.GetIdentifiedFillerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new IdentifiedFillerController(service.Object);

        var result = await controller.GetIdentifiedFiller(CancellationToken.None);

        Assert.Same(expected, result.Value);
    }
}
