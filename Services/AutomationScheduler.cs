
using Microsoft.Extensions.DependencyInjection;

namespace Karina.Api.Services;

public class AutomationScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomationScheduler> _logger;
    private readonly TimeZoneInfo _timeZone;
    private readonly TimeOnly _weatherTime;
    private readonly TimeOnly _recommendationTime;

    public AutomationScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<AutomationScheduler> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _timeZone = GetMonterreyTimeZone();

        _weatherTime = ReadScheduleTime(
            configuration,
            "Automations:Schedule:DailyWeather",
            "06:00");

        _recommendationTime = ReadScheduleTime(
            configuration,
            "Automations:Schedule:DailyRecommendation",
            "19:50");
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "AutomationScheduler iniciado. Zona horaria: {TimeZone}",
            _timeZone.Id);

        await Task.WhenAll(
            RunDailyAsync(
                "DailyWeather",
                _weatherTime,
                (service, token) =>
                    service.ExecuteDailyWeatherAsync(token),
                stoppingToken),

            RunDailyAsync(
                "DailyRecommendation",
                _recommendationTime,
                (service, token) =>
                    service.ExecuteDailyRecommendationAsync(token),
                stoppingToken)
        );

        _logger.LogInformation("AutomationScheduler detenido.");
    }

    private async Task RunDailyAsync(
        string automationName,
        TimeOnly scheduledTime,
        Func<IAutomationService, CancellationToken, Task<int>> execute,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            DateTimeOffset now = TimeZoneInfo.ConvertTime(
                DateTimeOffset.UtcNow,
                _timeZone);

            DateTime nextExecutionLocal =
                now.Date.Add(scheduledTime.ToTimeSpan());

            if (nextExecutionLocal <= now.DateTime)
            {
                nextExecutionLocal = nextExecutionLocal.AddDays(1);
            }

            DateTimeOffset nextExecution = new(
                nextExecutionLocal,
                _timeZone.GetUtcOffset(nextExecutionLocal));

            TimeSpan delay = nextExecution - DateTimeOffset.UtcNow;

            _logger.LogInformation(
                "{Automation} programado para {NextExecution}",
                automationName,
                nextExecution);

            try
            {
                await Task.Delay(delay, stoppingToken);

                using IServiceScope scope =
                    _scopeFactory.CreateScope();

                var automationService = scope.ServiceProvider
                    .GetRequiredService<IAutomationService>();

                _logger.LogInformation(
                    "Iniciando ejecución de {Automation}",
                    automationName);

                int sent = await execute(
                    automationService,
                    stoppingToken);

                _logger.LogInformation(
                    "{Automation} finalizado. Mensajes enviados: {Sent}",
                    automationName,
                    sent);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error durante la ejecución de {Automation}",
                    automationName);
            }
        }
    }

    private static TimeOnly ReadScheduleTime(
        IConfiguration configuration,
        string key,
        string defaultValue)
    {
        string value = configuration[key] ?? defaultValue;

        if (!TimeOnly.TryParseExact(
            value,
            "HH:mm",
            out TimeOnly time))
        {
            throw new InvalidOperationException(
                $"El horario configurado en {key} no es válido. " +
                "Utiliza el formato HH:mm.");
        }

        return time;
    }

    private static TimeZoneInfo GetMonterreyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "America/Monterrey");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Central Standard Time (Mexico)");
        }
    }
}
