using System;
using Jellyfin.Plugin.AnimeFillerSkipper.Models;
using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Controllers;

[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("AnimeFillerSkipper/SegmentPreferences")]
public class SegmentPreferencesController : ControllerBase
{
    private readonly IUserSegmentPreferenceService _preferenceService;

    public SegmentPreferencesController(IUserSegmentPreferenceService preferenceService)
    {
        _preferenceService = preferenceService;
    }

    [HttpGet("Unknown")]
    public ActionResult<SegmentPreferenceStatusDto> GetUnknownSegmentActionStatus()
    {
        return _preferenceService.GetUnknownSegmentActionStatus();
    }

    [HttpPost("Unknown")]
    public ActionResult<UpdateSegmentPreferenceResult> SetUnknownSegmentAction(
        [FromBody] UpdateSegmentPreferenceRequest request)
    {
        try
        {
            return _preferenceService.SetUnknownSegmentAction(request.UserIds, request.Action);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
