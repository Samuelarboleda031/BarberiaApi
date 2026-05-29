using BarberiaApi.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BarberiaApi.Infrastructure.Services;

public class NotificacionCitasService : INotificacionCitasService
{
    private readonly IEmailProxyService _emailProxy;
    private readonly IWhatsAppService _whatsApp;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificacionCitasService> _logger;

    public NotificacionCitasService(
        IEmailProxyService emailProxy,
        IWhatsAppService whatsApp,
        IConfiguration configuration,
        ILogger<NotificacionCitasService> logger)
    {
        _emailProxy = emailProxy;
        _whatsApp = whatsApp;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ResultadoNotificacionCita> NotificarCancelacionPorDesactivacionAsync(
        Agendamiento agendamiento,
        string motivo,
        IReadOnlyCollection<DateTime> sugerenciasReprogramacion)
    {
        var correo = agendamiento.Cliente?.Usuario?.Correo;
        ResultadoNotificacionCita emailResult;

        if (string.IsNullOrWhiteSpace(correo))
        {
            emailResult = new ResultadoNotificacionCita { Enviado = false, Canal = "smtp", Mensaje = "Cliente sin correo registrado." };
        }
        else
        {
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
            emailResult = new ResultadoNotificacionCita
            {
                Enviado = resultado.Enviado,
                Canal = "smtp",
                Mensaje = resultado.Mensaje
            };
        }

        if (_whatsApp.EstaHabilitado)
        {
            var telefono = agendamiento.Cliente?.Telefono;
            if (!string.IsNullOrWhiteSpace(telefono))
            {
                var clienteNombre = $"{agendamiento.Cliente?.Usuario?.Nombre} {agendamiento.Cliente?.Usuario?.Apellido}".Trim();
                var barberoNombre = $"{agendamiento.Barbero?.Usuario?.Nombre} {agendamiento.Barbero?.Usuario?.Apellido}".Trim();
                var fechaHora = agendamiento.FechaHora.ToString("dd/MM/yyyy HH:mm");
                var template = _configuration["WhatsApp:Templates:CancelacionCita"] ?? "cancelacion_cita";

                var wa = await _whatsApp.EnviarTemplateAsync(
                    telefono,
                    template,
                    new[] { clienteNombre, barberoNombre, fechaHora, motivo });
                if (wa.Enviado)
                    _logger.LogInformation("WhatsApp cancelación cita enviado a {Tel}.", telefono);
                else
                    _logger.LogWarning("Fallo WhatsApp cancelación cita a {Tel}: {Msg}", telefono, wa.Mensaje);
            }
        }

        return emailResult;
    }

    public async Task<ResultadoNotificacionCita> NotificarCancelacionGeneralAsync(
        Agendamiento agendamiento,
        string motivo)
    {
        return await NotificarCancelacionPorDesactivacionAsync(agendamiento, motivo, Array.Empty<DateTime>());
    }
}
