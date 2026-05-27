using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AnimeFillerSkipper.Models;
using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Controllers;

[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("AnimeFillerSkipper/IdentifiedFiller")]
public class IdentifiedFillerController : ControllerBase
{
    private readonly IIdentifiedFillerService _identifiedFillerService;

    public IdentifiedFillerController(IIdentifiedFillerService identifiedFillerService)
    {
        _identifiedFillerService = identifiedFillerService;
    }

    [HttpGet]
    public async Task<ActionResult<IdentifiedFillerResultDto>> GetIdentifiedFiller(CancellationToken cancellationToken)
    {
        return await _identifiedFillerService
            .GetIdentifiedFillerAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
