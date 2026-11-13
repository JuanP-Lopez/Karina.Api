using Karina.Api.DTOs.Automations;

public interface IAutomationService
{

    Task<int> ExecuteDailyWeatherAsync(CancellationToken cancellationToken = default);
    public Task<DailyWeather> GetDailyWeatherAsync(CancellationToken cancellationToken = default);

    Task<int> ExecuteDailyRecommendationAsync(CancellationToken cancellationToken = default);
    Task<DailyRecommendationDto> GetDailyRecommendationAsync(CancellationToken cancellationToken = default);
}