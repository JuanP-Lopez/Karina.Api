using Karina.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Karina.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutomationsController : ControllerBase
{
    private readonly IAutomationService _automationService;

    public AutomationsController(IAutomationService automationService)
    {
        _automationService = automationService;
    }

    [HttpGet("weather/preview")]
    public async Task<IActionResult> GetWeatherPreview(
        CancellationToken cancellationToken)
    {
        var weather = await _automationService.GetDailyWeatherAsync(
            cancellationToken);

        return Ok(weather);
    }

    [HttpPost("weather/executetest")]
    public async Task<IActionResult> ExecuteDailyWeather(CancellationToken cancellationToken)
    {
        int sent = await _automationService.ExecuteDailyWeatherAsync(cancellationToken);

        return Ok(new
        {
            message = "DailyWeather execution completed.",
            successfulMessages = sent
        });
    }

    //! Daily Recommendation
    [HttpGet("recommendation/preview")]
    public async Task<IActionResult> GetRecommendationPreview(
        CancellationToken cancellationToken)
    {
        var recommendation =
            await _automationService.GetDailyRecommendationAsync(
                cancellationToken);

        return Ok(recommendation);
    }

    [HttpPost("recommendation/executetest")]
    public async Task<IActionResult> ExecuteDailyRecommendation(
        CancellationToken cancellationToken)
    {
        int sent =
            await _automationService.ExecuteDailyRecommendationAsync(
                cancellationToken);

        return Ok(new
        {
            message = "DailyRecommendation execution completed.",
            successfulMessages = sent
        });
    }

    // Subscription management

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAutomations(
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out Guid userId))
            return Unauthorized();

        var automations = await _automationService.GetAutomationsAsync(
            userId,
            cancellationToken);

        return Ok(automations);
    }

    [Authorize]
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateAutomation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out Guid userId))
            return Unauthorized();

        bool result = await _automationService.ActivateAutomationAsync(
            userId,
            id,
            cancellationToken);

        if (!result)
            return NotFound(new { message = "Automation not found." });

        return Ok(new { message = "Automation activated successfully." });
    }

    [Authorize]
    [HttpDelete("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateAutomation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out Guid userId))
            return Unauthorized();

        bool result = await _automationService.DeactivateAutomationAsync(
            userId,
            id,
            cancellationToken);

        if (!result)
            return NotFound(new { message = "Automation not found." });

        return Ok(new { message = "Automation deactivated successfully." });
    }

}