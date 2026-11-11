namespace Karina.Api.DTOs.Automations;

public class DailyWeather
{
    public string Location { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public double TemperatureMax { get; set; }

    public double TemperatureMin { get; set; }

    public int? PrecipitationProbability { get; set; }

    public double? WindSpeed { get; set; }

    public string Conditions { get; set; } = string.Empty;
}