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

        await EnviarWhatsAppCancelacionAsync(agendamiento, motivo);

        return emailResult;
    }

    public async Task<ResultadoNotificacionCita> NotificarCancelacionGeneralAsync(
        Agendamiento agendamiento,
        string motivo)
    {
        return await NotificarCancelacionPorDesactivacionAsync(agendamiento, motivo, Array.Empty<DateTime>());
    }

    public Task NotificarCancelacionWhatsAppAsync(Agendamiento agendamiento, string motivo)
        => EnviarWhatsAppCancelacionAsync(agendamiento, motivo);

    public async Task NotificarCreacionAsync(Agendamiento agendamiento)
    {
        if (!_whatsApp.EstaHabilitado) return;

        var template = _configuration["WhatsApp:Templates:ConfirmacionCita"] ?? "confirmacion_cita";
        var parametros = ConstruirParametrosCita(agendamiento);

        await EnviarACliente(agendamiento, template, parametros, "creación cita");
        await EnviarABarbero(agendamiento, template, parametros, "creación cita");
        await EnviarAAdmin(template, parametros, "creación cita");
    }

    public async Task NotificarRecordatorioAsync(Agendamiento agendamiento)
    {
        if (!_whatsApp.EstaHabilitado) return;

        var template = _configuration["WhatsApp:Templates:RecordatorioCita"] ?? "recordatorio_cita";
        var parametros = ConstruirParametrosCita(agendamiento);

        await EnviarACliente(agendamiento, template, parametros, "recordatorio cita");
        await EnviarABarbero(agendamiento, template, parametros, "recordatorio cita");
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private async Task EnviarWhatsAppCancelacionAsync(Agendamiento agendamiento, string motivo)
    {
        if (!_whatsApp.EstaHabilitado) return;

        var template = _configuration["WhatsApp:Templates:CancelacionCita"] ?? "cancelacion_cita";
        var clienteNombre = NombreCliente(agendamiento);
        var barberoNombre = NombreBarbero(agendamiento);
        var fechaHora = agendamiento.FechaHora.ToString("dd/MM/yyyy HH:mm");
        var parametros = new[] { clienteNombre, barberoNombre, fechaHora, motivo };

        await EnviarACliente(agendamiento, template, parametros, "cancelación cita");
        await EnviarABarbero(agendamiento, template, parametros, "cancelación cita");
        await EnviarAAdmin(template, parametros, "cancelación cita");
    }

    private string[] ConstruirParametrosCita(Agendamiento agendamiento)
    {
        var clienteNombre = NombreCliente(agendamiento);
        var barberoNombre = NombreBarbero(agendamiento);
        var fechaHora = agendamiento.FechaHora.ToString("dd/MM/yyyy HH:mm");
        var servicioNombre = agendamiento.Servicio?.Nombre ?? agendamiento.Paquete?.Nombre ?? "Servicio";
        return new[] { clienteNombre, barberoNombre, fechaHora, servicioNombre };
    }

    private Task EnviarACliente(Agendamiento agendamiento, string template, IReadOnlyList<string> parametros, string contexto)
        => EnviarA(agendamiento.Cliente?.Telefono, template, parametros, contexto, "cliente");

    private Task EnviarABarbero(Agendamiento agendamiento, string template, IReadOnlyList<string> parametros, string contexto)
        => EnviarA(agendamiento.Barbero?.Telefono, template, parametros, contexto, "barbero");

    private Task EnviarAAdmin(string template, IReadOnlyList<string> parametros, string contexto)
        => EnviarA(_configuration["WhatsApp:AdminTelefono"], template, parametros, contexto, "admin");

    private async Task EnviarA(
        string? telefono,
        string template,
        IReadOnlyList<string> parametros,
        string contexto,
        string destinatario)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return;

        try
        {
            var wa = await _whatsApp.EnviarTemplateAsync(telefono, template, parametros);
            if (wa.Enviado)
                _logger.LogInformation("WhatsApp {Contexto} enviado a {Destinatario} ({Tel}).", contexto, destinatario, telefono);
            else
                _logger.LogWarning("Fallo WhatsApp {Contexto} a {Destinatario} ({Tel}): {Msg}", contexto, destinatario, telefono, wa.Mensaje);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción WhatsApp {Contexto} a {Destinatario} ({Tel}).", contexto, destinatario, telefono);
        }
    }

    private static string NombreCliente(Agendamiento a)
        => $"{a.Cliente?.Usuario?.Nombre} {a.Cliente?.Usuario?.Apellido}".Trim();

    private static string NombreBarbero(Agendamiento a)
        => $"{a.Barbero?.Usuario?.Nombre} {a.Barbero?.Usuario?.Apellido}".Trim();
}
