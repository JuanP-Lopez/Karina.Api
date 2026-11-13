using Karina.Api.Services;
using Microsoft.AspNetCore.Mvc;

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

}