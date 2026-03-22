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
}

public class ResultadoNotificacionCita
{
    public bool Enviado { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}
