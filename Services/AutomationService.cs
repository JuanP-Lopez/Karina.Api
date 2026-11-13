using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Karina.Api.Data;
using Karina.Api.Services;
using Karina.Api.DTOs.Automations;
using Karina.Api.Models.Automations;

public class AutomationService : IAutomationService
{
    private readonly KarinaContext _context;

    private readonly IWhatsAppService _whatsAppService;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ISessionService _sessionService;

    private readonly ICommunicationService _communicationService;

    private readonly IConfiguration _configuration;

    public AutomationService(
        KarinaContext context,
        IWhatsAppService whatsAppService,
        IHttpClientFactory httpClientFactory,
        ISessionService sessionService,
        ICommunicationService communicationService,
        IConfiguration configuration)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _httpClientFactory = httpClientFactory;
        _sessionService = sessionService;
        _communicationService = communicationService;
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

    //! Daily recommendation

    public async Task<DailyRecommendationDto> GetDailyRecommendationAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (created, sessionId) = await _sessionService.createSession(
                SystemUsers.AgentUserId,
                SystemUsers.KarinaAutomation
            );

            if (!created || string.IsNullOrWhiteSpace(sessionId))
            {
                Console.WriteLine("=== DailyRecommendation: Could Not Create ADK Session ===");

                return GetFallbackRecommendation();
            }

            string prompt = """
            Realiza una recomendacion de entretenimiento.

            Categorías permitidas:
            - Movies
            - Series
            - VideoGames
            - Music
            - Anime
            - Other

            Instrucciones:
            1. Selecciona una obra real.
            2. Procura variar las categorías entre ejecuciones.
            3. No inventes informacion.
            4. Si tienes acceso a búsqueda, verifica la información.
            5. Utiliza una descripción breve y atractiva.
            7. Devuelve exclusivamente un objeto JSON válido.
            8. No utilices Markdown ni bloques de código.

            Estructura obligatoria:
            {
                "category": "",
                "title": "",
                "year": null,
                "description": "",
                "reason": "",
                "sourceUrl": null
            }
            """;

            string? response = await _communicationService.GenerateAutomationResponseAsync(
                    SystemUsers.AgentUserId,
                    sessionId,
                    prompt,
                    cancellationToken
                );

            if (string.IsNullOrWhiteSpace(response))
            {
                return GetFallbackRecommendation();
            }

            var recommendation = ParseRecommendation(response);

            if (recommendation == null)
            {
                Console.WriteLine(
                    "=== DailyRecommendation: Invalid Agent Response ==="
                );

                return GetFallbackRecommendation();
            }

            return recommendation;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"=== DailyRecommendation Generation Error: {ex.Message} ==="
            );

            return GetFallbackRecommendation();
        }
    }


    private static DailyRecommendationDto? ParseRecommendation(
        string response)
    {
        try
        {
            string json = response.Trim();

            // Eliminar bloques Markdown si el agente los devuelve.
            if (json.StartsWith("```"))
            {
                int firstLine = json.IndexOf('\n');

                if (firstLine >= 0)
                    json = json[(firstLine + 1)..];

                if (json.EndsWith("```"))
                    json = json[..^3];

                json = json.Trim();
            }

            var recommendation =
                JsonSerializer.Deserialize<DailyRecommendationDto>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

            if (recommendation == null ||
                string.IsNullOrWhiteSpace(recommendation.Category) ||
                string.IsNullOrWhiteSpace(recommendation.Title) ||
                string.IsNullOrWhiteSpace(recommendation.Description) ||
                string.IsNullOrWhiteSpace(recommendation.Reason))
            {
                return null;
            }

            string[] validCategories =
            [
                "Movies",
            "Series",
            "VideoGames",
            "Music",
            "Anime",
            "Other"
            ];

            if (!validCategories.Contains(
                recommendation.Category,
                StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            // Solo aceptamos URLs HTTP o HTTPS.
            if (!string.IsNullOrWhiteSpace(recommendation.SourceUrl))
            {
                bool validUrl = Uri.TryCreate(
                    recommendation.SourceUrl,
                    UriKind.Absolute,
                    out Uri? uri
                ) && (uri.Scheme == Uri.UriSchemeHttp ||
                      uri.Scheme == Uri.UriSchemeHttps);

                if (!validUrl)
                    recommendation.SourceUrl = null;
            }

            return recommendation;
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                $"=== DailyRecommendation JSON Error: {ex.Message} ==="
            );

            return null;
        }
    }


    private static DailyRecommendationDto GetFallbackRecommendation()
    {
        DailyRecommendationDto[] recommendations =
        [
            new()
        {
            Category = "VideoGames",
            Title = "Outer Wilds",
            Year = 2019,
            Description =
                "Una aventura de exploración espacial donde descubrirás " +
                "los secretos de un sistema solar atrapado en un bucle temporal.",
            Reason =
                "Una experiencia original que recompensa la curiosidad.",
            SourceUrl =
                "https://store.steampowered.com/app/753640/Outer_Wilds/"
        },
        new()
        {
            Category = "Movies",
            Title = "The Truman Show",
            Year = 1998,
            Description =
                "Un hombre descubre que su vida aparentemente normal " +
                "forma parte de un programa televisivo.",
            Reason =
                "Una película que invita a reflexionar sobre la realidad.",
            SourceUrl = null
        },
        new()
        {
            Category = "Anime",
            Title = "Violet Evergarden",
            Year = 2018,
            Description =
                "Una joven aprende a comprender las emociones humanas " +
                "a través de las cartas que escribe para otras personas.",
            Reason =
                "Una historia visualmente hermosa y emocional.",
            SourceUrl = null
        },
        new()
        {
            Category = "Series",
            Title = "Severance",
            Year = 2022,
            Description =
                "Los empleados de una empresa separan quirúrgicamente " +
                "sus recuerdos laborales de los personales.",
            Reason =
                "Una propuesta de ciencia ficción con una premisa fascinante.",
            SourceUrl = null
        }
        ];

        return recommendations[
            Random.Shared.Next(recommendations.Length)
        ];
    }


    public async Task<int> ExecuteDailyRecommendationAsync(
        CancellationToken cancellationToken = default)
    {
        // 1. Obtener los suscriptores.
        var subscribers = await _context.UsersAutomations
            .Where(ua => ua.Automation.name == "DailyRecommendation")
            .Select(ua => ua.User.wa_id)
            .Where(waId => !string.IsNullOrWhiteSpace(waId))
            .Distinct()
            .ToListAsync(cancellationToken);

        if (subscribers.Count == 0)
        {
            Console.WriteLine(
                "=== DailyRecommendation: No Subscribers Found ==="
            );

            return 0;
        }

        // 2. Generar una única recomendación.
        var recommendation = await GetDailyRecommendationAsync(
            cancellationToken
        );

        // 3. Formatear el mensaje.
        string message = FormatDailyRecommendation(recommendation);

        int successfulMessages = 0;

        // 4. Distribuir a todos los suscriptores.
        foreach (var recipient in subscribers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                bool sent = await _whatsAppService.SendTextMessageAsync(
                    recipient,
                    message,
                    cancellationToken
                );

                if (sent)
                    successfulMessages++;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"=== DailyRecommendation Error Sending to " +
                    $"{recipient}: {ex.Message} ==="
                );
            }
        }

        return successfulMessages;
    }


    private static string FormatDailyRecommendation(
        DailyRecommendationDto recommendation)
    {
        string category = recommendation.Category switch
        {
            "Movies" => "Película",
            "Series" => "Serie",
            "VideoGames" => "Videojuego",
            "Music" => "Música",
            "Anime" => "Anime",
            _ => "Entretenimiento"
        };

        string year = recommendation.Year.HasValue
            ? $" ({recommendation.Year})"
            : "";

        string source = !string.IsNullOrWhiteSpace(
            recommendation.SourceUrl)
            ? $"\n\n🔗 Más información:\n{recommendation.SourceUrl}"
            : "";

        return $"""
        🎁 *Recomendación del día*

        🎭 *Categoría:* {category}

        ✨ *{recommendation.Title}{year}*

        📝 {recommendation.Description}

        💡 *¿Por qué te la recomendamos?*
        {recommendation.Reason}{source}

        ¡Esperamos que la disfrutes! 🌟
        """;
    }



}