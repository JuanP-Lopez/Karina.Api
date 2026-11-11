using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Karina.Api.Data;
using Karina.Api.Services;
using Karina.Api.DTOs.Automations;

public class AutomationService : IAutomationService
{
    private readonly KarinaContext _context;

    private readonly IWhatsAppService _whatsAppService;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly IConfiguration _configuration;

    public AutomationService(
        KarinaContext context,
        IWhatsAppService whatsAppService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<int> ExecuteDailyWeatherAsync(CancellationToken cancellationToken = default)
    {
        // 1. Obtener el pronóstico del día.
        var weather = await GetDailyWeatherAsync(cancellationToken);

        // 2. Construir el mensaje.
        string message = FormatDailyWeather(weather);

        // 3. Recuperar los usuarios suscritos.
        var subscribers = await _context.UsersAutomations
            .Where(ua => ua.Automation.name == "DailyWeather")
            .Select(ua => ua.User.wa_id)
            .Where(waId => waId != "")
            .Distinct()
            .ToListAsync(cancellationToken);

        int successfulMessages = 0;

        // 4. Distribuir el mismo mensaje a todos los suscriptores.
        foreach (var recipient in subscribers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                bool sent = await _whatsAppService.SendTextMessageAsync(
                    recipient,
                    message,
                    cancellationToken);

                if (sent)
                {
                    successfulMessages++;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"=== DailyWeather Error Sending to {recipient}: {ex.Message} ===");
            }
        }

        return successfulMessages;
    }

    public async Task<DailyWeather> GetDailyWeatherAsync(
       CancellationToken cancellationToken = default)
    {
        var settings = _configuration.GetSection(
            "Automations:DailyWeather");

        double latitude = settings.GetValue<double>("Latitude");
        double longitude = settings.GetValue<double>("Longitude");

        string location = settings["Location"]
            ?? "Nuevo León, México";

        string timezone = settings["Timezone"]
            ?? "America/Monterrey";

        string url =
            $"v1/forecast?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
            "&daily=temperature_2m_max,temperature_2m_min," +
            "precipitation_probability_max,wind_speed_10m_max,weather_code" +
            $"&timezone={Uri.EscapeDataString(timezone)}" +
            "&forecast_days=1";

        var client = _httpClientFactory.CreateClient("OpenMeteo");

        var response = await client.GetFromJsonAsync<OpenMeteoResponse>(
            url,
            cancellationToken);

        var daily = response?.Daily;

        if (daily?.Time?.Length == 0 ||
            daily?.TemperatureMax?.Length == 0 ||
            daily?.TemperatureMin?.Length == 0 ||
            daily?.WeatherCode?.Length == 0)
        {
            throw new InvalidOperationException(
                "Open-Meteo no devolvió los datos meteorológicos esperados.");
        }

        return new DailyWeather
        {
            Location = location,
            Date = DateOnly.ParseExact(
                daily!.Time![0],
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture),

            TemperatureMax = daily.TemperatureMax![0]
                ?? throw new InvalidOperationException(
                    "No se recibió la temperatura máxima."),

            TemperatureMin = daily.TemperatureMin![0]
                ?? throw new InvalidOperationException(
                    "No se recibió la temperatura mínima."),

            PrecipitationProbability =
                daily.PrecipitationProbability?.FirstOrDefault(),

            WindSpeed = daily.WindSpeed?.FirstOrDefault(),

            Conditions = GetWeatherDescription(
                daily.WeatherCode![0]!.Value)
        };
    }

    private static string GetWeatherDescription(int code)
    {
        return code switch
        {
            0 => "Despejado",
            1 => "Mayormente despejado",
            2 => "Parcialmente nublado",
            3 => "Nublado",
            45 or 48 => "Niebla",
            51 or 53 or 55 => "Llovizna",
            56 or 57 => "Llovizna helada",
            61 or 63 or 65 => "Lluvia",
            66 or 67 => "Lluvia helada",
            71 or 73 or 75 => "Nevada",
            77 => "Granos de nieve",
            80 or 81 or 82 => "Chubascos",
            85 or 86 => "Chubascos de nieve",
            95 => "Tormenta",
            96 or 99 => "Tormenta con granizo",
            _ => "Condiciones meteorológicas variables"
        };
    }

    private string FormatDailyWeather(DailyWeather weather)
    {
        return $"""
        🌤️ *Pronóstico diario del clima*

        📍 {weather.Location}
        📅 {weather.Date:dd/MM/yyyy}

        🌡️ Temperatura:
        • Máxima: {weather.TemperatureMax}°C
        • Mínima: {weather.TemperatureMin}°C

        🌧️ Probabilidad de lluvia:
        {weather.PrecipitationProbability?.ToString() ?? "No disponible"}%

        💨 Viento:
        {weather.WindSpeed?.ToString() ?? "No disponible"} km/h

        ☁️ Condiciones:
        {weather.Conditions}
        """;
    }

    private class OpenMeteoResponse
    {
        [JsonPropertyName("daily")]
        public DailyForecast? Daily { get; set; }
    }

    private class DailyForecast
    {
        [JsonPropertyName("time")]
        public string[]? Time { get; set; }

        [JsonPropertyName("temperature_2m_max")]
        public double?[]? TemperatureMax { get; set; }

        [JsonPropertyName("temperature_2m_min")]
        public double?[]? TemperatureMin { get; set; }

        [JsonPropertyName("precipitation_probability_max")]
        public int?[]? PrecipitationProbability { get; set; }

        [JsonPropertyName("wind_speed_10m_max")]
        public double?[]? WindSpeed { get; set; }

        [JsonPropertyName("weather_code")]
        public int?[]? WeatherCode { get; set; }
    }
}