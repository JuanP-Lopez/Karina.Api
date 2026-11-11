namespace Karina.Api.DTOs.Automations;

public class DailyRecommendationDto
{
    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string? Platform { get; set; }

    public string? SourceUrl { get; set; }
}