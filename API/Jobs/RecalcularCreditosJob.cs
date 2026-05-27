using BarberiaApi.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BarberiaApi.Jobs;

/// <summary>
/// Background job que recalcula cada 5 minutos los créditos vencidos de barberos.
/// Cambia el estado a BloqueadoVencimiento cuando la FechaVencimiento se supera.
/// </summary>
public sealed class RecalcularCreditosJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecalcularCreditosJob> _logger;

    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(5);

    public RecalcularCreditosJob(
        IServiceScopeFactory scopeFactory,
        ILogger<RecalcularCreditosJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecalcularCreditosJob iniciado.");

        // Primera ejecución al arrancar
        await EjecutarAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Intervalo, stoppingToken);
            await EjecutarAsync(stoppingToken);
        }

        _logger.LogInformation("RecalcularCreditosJob detenido.");
    }

    private async Task EjecutarAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<ICreditoBarberoService>();
            await servicio.RecalcularEstadosVencidosAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al recalcular estados vencidos de créditos barbero.");
        }
    }
}
