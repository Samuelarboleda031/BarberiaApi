using System.Globalization;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BarberiaApi.Infrastructure.Services;

public sealed class EmailProxyService : IEmailProxyService
{
    private readonly ILogger<EmailProxyService> _logger;
    private readonly IConfiguration _configuration;

    public EmailProxyService(
        ILogger<EmailProxyService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ProxyEmailResult> EnviarCancelacionAsync(
        CancelacionEmailProxyRequest request,
        CancellationToken cancellationToken = default)
    {
        var habilitadoRaw = GetConfig("Smtp:Habilitado", "Notificaciones:Correo:Habilitado");
        var habilitado = string.IsNullOrWhiteSpace(habilitadoRaw) ||
                         (bool.TryParse(habilitadoRaw, out var parsedEnabled) && parsedEnabled);
        if (!habilitado)
        {
            return new ProxyEmailResult
            {
                Enviado = false,
                CodigoRespuesta = 503,
                Mensaje = "Envío de correo deshabilitado por configuración."
            };
        }

        var host = GetConfig("Smtp:Host", "Notificaciones:Correo:Host");
        var username = GetConfig("Smtp:Username", "Notificaciones:Correo:Usuario");
        var password = GetConfig("Smtp:Password", "Notificaciones:Correo:Contrasena");
        var fromEmail = GetConfig("Smtp:FromEmail", "Notificaciones:Correo:Remitente");
        var fromName = GetConfig("Smtp:FromName", "Notificaciones:Correo:NombreRemitente") ?? "Barbería App";
        var portRaw = GetConfig("Smtp:Port", "Notificaciones:Correo:Puerto");
        var sslRaw = GetConfig("Smtp:EnableSsl", "Notificaciones:Correo:UseSsl");
        var port = int.TryParse(portRaw, out var parsedPort) ? parsedPort : 587;
        var enableSsl = !string.IsNullOrWhiteSpace(sslRaw) && bool.TryParse(sslRaw, out var parsedSsl)
            ? parsedSsl
            : true;

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            return new ProxyEmailResult
            {
                Enviado = false,
                CodigoRespuesta = 500,
                Mensaje = "Configuración SMTP incompleta en backend."
            };
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password),
            };

            var appName = string.IsNullOrWhiteSpace(request.AppName) ? "Barbería App" : request.AppName;
            var fechaFormateada = FormatFecha(request.FechaOriginal);
            var sugerencias = request.SugerenciasReprogramacion is { Count: > 0 }
                ? string.Join(" | ", request.SugerenciasReprogramacion.Select(FormatFecha))
                : "No disponibles";

            var subject = $"{appName} - Cancelación de cita";
            var body = $@"
Hola {WebUtility.HtmlEncode(request.ClienteNombre)},

Tu cita fue cancelada.

Barbero: {WebUtility.HtmlEncode(request.BarberoNombre)}
Fecha original: {WebUtility.HtmlEncode(fechaFormateada)}
Motivo: {WebUtility.HtmlEncode(request.Motivo)}
Sugerencias de reprogramación: {WebUtility.HtmlEncode(sugerencias)}

Gracias por tu comprensión.
{WebUtility.HtmlEncode(appName)}";

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(new MailAddress(request.ClienteEmail, request.ClienteNombre));

            await smtpClient.SendMailAsync(message);

            return new ProxyEmailResult
            {
                Enviado = true,
                CodigoRespuesta = 200,
                Mensaje = "Correo enviado vía SMTP desde backend."
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP proxy falló al enviar correo de cancelación.");
            return new ProxyEmailResult
            {
                Enviado = false,
                CodigoRespuesta = 500,
                Mensaje = $"SMTP rechazó la solicitud: {ex.Message}"
            };
        }
    }

    private static string FormatFecha(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        if (!DateTime.TryParse(input, out var dt))
            return input;

        return dt.ToString("dddd, d 'de' MMMM, hh:mm tt", new CultureInfo("es-ES"));
    }

    private string? GetConfig(string primary, string fallback)
    {
        var value = _configuration[primary];
        if (!string.IsNullOrWhiteSpace(value)) return value;
        return _configuration[fallback];
    }
}
