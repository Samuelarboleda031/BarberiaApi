using BarberiaApi.Models;

namespace BarberiaApi.Services;

public class NotificacionCitasService : INotificacionCitasService
{
    private readonly IConfiguration _configuration;

    public NotificacionCitasService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<ResultadoNotificacionCita> NotificarCancelacionPorDesactivacionAsync(
        Agendamiento agendamiento,
        string motivo,
        IReadOnlyCollection<DateTime> sugerenciasReprogramacion)
    {
        var correoHabilitado = _configuration.GetValue<bool>("Notificaciones:Correo:Habilitado", false);
        var resultado = new ResultadoNotificacionCita
        {
            Enviado = !correoHabilitado,
            Canal = correoHabilitado ? "correo_pendiente_integracion" : "in_app",
            Mensaje = correoHabilitado
                ? "Cancelación registrada. El canal correo está marcado como habilitado, pero no existe implementación SMTP activa en este proyecto."
                : "Cancelación notificada para consumo frontend (in-app), sin envío de correo."
        };

        return Task.FromResult(resultado);
    }
}
