using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Infrastructure.Services;

public class NotificacionCitasService : INotificacionCitasService
{
    private readonly IEmailProxyService _emailProxy;

    public NotificacionCitasService(IEmailProxyService emailProxy)
    {
        _emailProxy = emailProxy;
    }

    public async Task<ResultadoNotificacionCita> NotificarCancelacionPorDesactivacionAsync(
        Agendamiento agendamiento,
        string motivo,
        IReadOnlyCollection<DateTime> sugerenciasReprogramacion)
    {
        var correo = agendamiento.Cliente?.Usuario?.Correo;
        if (string.IsNullOrWhiteSpace(correo))
            return new ResultadoNotificacionCita { Enviado = false, Canal = "smtp", Mensaje = "Cliente sin correo registrado." };

        var request = new CancelacionEmailProxyRequest
        {
            ClienteNombre = $"{agendamiento.Cliente?.Usuario?.Nombre} {agendamiento.Cliente?.Usuario?.Apellido}".Trim(),
            ClienteEmail = correo,
            BarberoNombre = $"{agendamiento.Barbero?.Usuario?.Nombre} {agendamiento.Barbero?.Usuario?.Apellido}".Trim(),
            FechaOriginal = agendamiento.FechaHora.ToString("o"),
            Motivo = motivo,
            SugerenciasReprogramacion = sugerenciasReprogramacion.Select(s => s.ToString("o")).ToList()
        };

        var resultado = await _emailProxy.EnviarCancelacionAsync(request);
        return new ResultadoNotificacionCita
        {
            Enviado = resultado.Enviado,
            Canal = "smtp",
            Mensaje = resultado.Mensaje
        };
    }

    public async Task<ResultadoNotificacionCita> NotificarCancelacionGeneralAsync(
        Agendamiento agendamiento,
        string motivo)
    {
        return await NotificarCancelacionPorDesactivacionAsync(agendamiento, motivo, Array.Empty<DateTime>());
    }
}
