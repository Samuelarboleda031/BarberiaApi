using BarberiaApi.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace BarberiaApi.Infrastructure.Services;

/// <summary>
/// El servicio de notificacion via SMTP/Resend ha sido desactivado en el backend.
/// Las notificaciones de cancelacion ahora se gestionan en el frontend mediante EmailJS.
/// </summary>
    public class NotificacionCitasService : INotificacionCitasService
    {
        public NotificacionCitasService()
        {
        }

    public async Task<ResultadoNotificacionCita> NotificarCancelacionPorDesactivacionAsync(
        Agendamiento agendamiento,
        string motivo,
        IReadOnlyCollection<DateTime> sugerenciasReprogramacion)
    {
        return await Task.FromResult(new ResultadoNotificacionCita
        {
            Enviado = false,
            Canal = "frontend",
            Mensaje = "Notificacion pendiente de envio via EmailJS (Frontend)."
        });
    }

    public async Task<ResultadoNotificacionCita> NotificarCancelacionGeneralAsync(
        Agendamiento agendamiento,
        string motivo)
    {
        return await Task.FromResult(new ResultadoNotificacionCita
        {
            Enviado = false,
            Canal = "frontend",
            Mensaje = "Notificacion pendiente de envio via EmailJS (Frontend)."
        });
    }
}
