using BarberiaApi.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BarberiaApi.Infrastructure.Jobs;

public sealed class NotificacionCreditoJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificacionCreditoJob> _logger;

    // Cada 48 horas notifica a barberos bloqueados (RN-065)
    private static readonly TimeSpan IntervaloBloqueados = TimeSpan.FromHours(48);

    // Resumen semanal los lunes a las 8:00 AM (RN-066)
    private static readonly TimeSpan HoraResumenSemanal = new(8, 0, 0);

    private DateTime _proximoResumenSemanal;

    public NotificacionCreditoJob(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificacionCreditoJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _proximoResumenSemanal = CalcularProximoLunes();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificacionCreditoJob iniciado.");

        var proximaBloqueados = DateTime.UtcNow.Add(IntervaloBloqueados);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            var ahora = DateTime.UtcNow;

            if (ahora >= proximaBloqueados)
            {
                proximaBloqueados = ahora.Add(IntervaloBloqueados);
                await EjecutarNotificacionBloqueadosAsync(stoppingToken);
            }

            if (ahora >= _proximoResumenSemanal)
            {
                _proximoResumenSemanal = _proximoResumenSemanal.AddDays(7);
                await EjecutarResumenSemanalAsync(stoppingToken);
            }
        }

        _logger.LogInformation("NotificacionCreditoJob detenido.");
    }

    private async Task EjecutarNotificacionBloqueadosAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<INotificacionCreditoService>();
            await servicio.NotificarBloqueadosActivosAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al ejecutar notificaciones periódicas de créditos bloqueados.");
        }
    }

    private async Task EjecutarResumenSemanalAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<INotificacionCreditoService>();
            await servicio.EnviarResumenSemanalAdminAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al ejecutar resumen semanal de créditos.");
        }
    }

    private static DateTime CalcularProximoLunes()
    {
        var ahora = DateTime.UtcNow;
        var diasHastaLunes = ((int)DayOfWeek.Monday - (int)ahora.DayOfWeek + 7) % 7;
        if (diasHastaLunes == 0 && ahora.TimeOfDay >= HoraResumenSemanal)
            diasHastaLunes = 7;

        return ahora.Date.AddDays(diasHastaLunes).Add(HoraResumenSemanal);
    }
}
