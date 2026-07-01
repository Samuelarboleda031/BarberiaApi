using BarberiaApi.Domain.Constants;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BarberiaApi.Infrastructure.Jobs;

/// <summary>
/// Envía un recordatorio por WhatsApp (cliente + barbero) cuando una cita Pendiente/Confirmada
/// está a 30 minutos o menos de comenzar. El control anti-duplicado es en memoria: si el
/// servidor se reinicia dentro de la ventana, una cita podría recibir el recordatorio dos veces.
/// </summary>
public sealed class RecordatorioCitasJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecordatorioCitasJob> _logger;

    // Minutos antes del inicio en que se considera "próxima" la cita.
    private static readonly TimeSpan VentanaRecordatorio = TimeSpan.FromMinutes(30);

    // Frecuencia con que se revisa la agenda.
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);

    // Zona horaria de Colombia (mismo criterio que Application.Common.DateTimeProvider).
    private static readonly TimeZoneInfo BogotaTz =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Bogota");

    // IDs de citas ya recordadas (anti-duplicado en memoria).
    private readonly HashSet<int> _recordadas = new();

    public RecordatorioCitasJob(
        IServiceScopeFactory scopeFactory,
        ILogger<RecordatorioCitasJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecordatorioCitasJob iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Intervalo, stoppingToken);

            try
            {
                await EjecutarAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error al ejecutar recordatorios de citas.");
            }
        }

        _logger.LogInformation("RecordatorioCitasJob detenido.");
    }

    private async Task EjecutarAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BarberiaContext>();
        var notificacion = scope.ServiceProvider.GetRequiredService<INotificacionCitasService>();

        var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BogotaTz);
        var limite = ahora.Add(VentanaRecordatorio);

        // Citas activas cuyo inicio cae dentro de los próximos 30 minutos.
        var proximas = await context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.Servicio)
            .Include(a => a.AgendamientoServicios)
            .Include(a => a.AgendamientoProductos)
            .Include(a => a.Paquete)
            .Where(a =>
                (a.Estado == EstadosAgendamiento.Pendiente || a.Estado == EstadosAgendamiento.Confirmada) &&
                a.FechaHora > ahora &&
                a.FechaHora <= limite)
            .ToListAsync(ct);

        foreach (var cita in proximas)
        {
            if (_recordadas.Contains(cita.Id)) continue;

            await notificacion.NotificarRecordatorioAsync(cita);
            _recordadas.Add(cita.Id);
            _logger.LogInformation("Recordatorio enviado para cita {CitaId} ({FechaHora}).", cita.Id, cita.FechaHora);
        }

        // Conserva solo las citas que siguen dentro de la ventana; las ya pasadas se descartan
        // para que el set no crezca indefinidamente.
        var idsVigentes = proximas.Select(c => c.Id).ToHashSet();
        _recordadas.RemoveWhere(id => !idsVigentes.Contains(id));
    }
}
