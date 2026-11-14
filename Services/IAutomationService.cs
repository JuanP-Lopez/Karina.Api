using Karina.Api.DTOs.Automations;

public interface IAutomationService
{

    Task<int> ExecuteDailyWeatherAsync(CancellationToken cancellationToken = default);
    public Task<DailyWeather> GetDailyWeatherAsync(CancellationToken cancellationToken = default);

    Task<int> ExecuteDailyRecommendationAsync(CancellationToken cancellationToken = default);
    Task<DailyRecommendationDto> GetDailyRecommendationAsync(CancellationToken cancellationToken = default);

    Task<List<AutomationDto>> GetAutomationsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ActivateAutomationAsync(Guid userId, Guid automationId, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAutomationAsync(Guid userId, Guid automationId, CancellationToken cancellationToken = default);
}