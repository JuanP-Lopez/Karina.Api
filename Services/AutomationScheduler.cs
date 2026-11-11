using Microsoft.Extensions.DependencyInjection;

namespace Karina.Api.Services;

public class AutomationScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomationScheduler> _logger;
    private readonly TimeZoneInfo _timeZone;

    public AutomationScheduler(IServiceScopeFactory scopeFactory, ILogger<AutomationScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _timeZone = GetMonterreyTimeZone();
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "AutomationScheduler iniciado. Zona horaria: {TimeZone}",
            _timeZone.Id);

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTimeOffset now = TimeZoneInfo.ConvertTime(
                DateTimeOffset.UtcNow,
                _timeZone);

            //! Próxima ejecución en formato de 24 horas
            DateTime nextExecutionLocal = now.Date.AddHours(6);

            if (nextExecutionLocal <= now.DateTime)
            {
                nextExecutionLocal = nextExecutionLocal.AddDays(1);
            }

            DateTimeOffset nextExecution = new(
                nextExecutionLocal,
                _timeZone.GetUtcOffset(nextExecutionLocal));

            TimeSpan delay = nextExecution - DateTimeOffset.UtcNow;

            _logger.LogInformation(
                "DailyWeather programado para {NextExecution}",
                nextExecution);

            try
            {
                await Task.Delay(delay, stoppingToken);

                using IServiceScope scope = _scopeFactory.CreateScope();

                var automationService = scope.ServiceProvider
                    .GetRequiredService<IAutomationService>();

                _logger.LogInformation(
                    "Iniciando ejecución de DailyWeather.");

                int sent = await automationService.ExecuteDailyWeatherAsync(
                    stoppingToken);

                _logger.LogInformation(
                    "DailyWeather finalizado. Mensajes enviados: {Sent}",
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
                    "Error durante la ejecución de DailyWeather.");
            }
        }

        _logger.LogInformation("AutomationScheduler detenido.");
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
            // Alternativa para entornos Windows.
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Central Standard Time (Mexico)");
        }
    }
}
