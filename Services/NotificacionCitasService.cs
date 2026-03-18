using BarberiaApi.Models;

namespace BarberiaApi.Services;

/// <summary>
/// El servicio de notificación vía SMTP/Resend ha sido desactivado en el backend.
/// Las notificaciones de cancelación ahora se gestionan en el frontend mediante EmailJS.
/// </summary>
public class NotificacionCitasService : INotificacionCitasService
{
    private readonly IConfiguration _configuration;

    public NotificacionCitasService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ResultadoNotificacionCita> NotificarCancelacionPorDesactivacionAsync(
        Agendamiento agendamiento,
        string motivo,
        IReadOnlyCollection<DateTime> sugerenciasReprogramacion)
    {
        // Notificación desactivada en backend. El frontend maneja esto con EmailJS.
        return await Task.FromResult(new ResultadoNotificacionCita
        {
            Enviado = false,
            Canal = "frontend",
            Mensaje = "Notificación pendiente de envío vía EmailJS (Frontend)."
        });
    }

    public async Task<ResultadoNotificacionCita> NotificarCancelacionGeneralAsync(
        Agendamiento agendamiento,
        string motivo)
    {
        // Notificación desactivada en backend. El frontend maneja esto con EmailJS.
        return await Task.FromResult(new ResultadoNotificacionCita
        {
            Enviado = false,
            Canal = "frontend",
            Mensaje = "Notificación pendiente de envío vía EmailJS (Frontend)."
        });
    }
}
