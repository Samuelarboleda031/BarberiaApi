using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Infrastructure.Services;

public interface INotificacionCitasService
{
    Task<ResultadoNotificacionCita> NotificarCancelacionPorDesactivacionAsync(
        Agendamiento agendamiento,
        string motivo,
        IReadOnlyCollection<DateTime> sugerenciasReprogramacion);

    Task<ResultadoNotificacionCita> NotificarCancelacionGeneralAsync(
        Agendamiento agendamiento,
        string motivo);

    /// <summary>
    /// Envía únicamente la notificación de cancelación por WhatsApp a cliente, barbero
    /// y administrador. No envía correo (lo gestiona AgendamientoService por separado).
    /// </summary>
    Task NotificarCancelacionWhatsAppAsync(Agendamiento agendamiento, string motivo);

    /// <summary>
    /// Notifica por WhatsApp la creación/confirmación de una cita a cliente, barbero y administrador.
    /// </summary>
    Task NotificarCreacionAsync(Agendamiento agendamiento);

    /// <summary>
    /// Notifica por WhatsApp el recordatorio de una cita próxima a cliente y barbero.
    /// </summary>
    Task NotificarRecordatorioAsync(Agendamiento agendamiento);
}

public class ResultadoNotificacionCita
{
    public bool Enviado { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}
