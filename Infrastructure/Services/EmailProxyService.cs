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
            var bookingUrl = GetConfig("Notificaciones:Correo:UrlReserva", "Smtp:BookingUrl")
                             ?? "https://front4-tu-app.web.app/";

            var subject = $"{appName} - Cancelación de cita";
            var body = BuildCancellationHtml(
                toName: request.ClienteNombre,
                appName: appName,
                motivo: request.Motivo,
                barberoName: request.BarberoNombre,
                fechaHora: fechaFormateada,
                sugerencias: sugerencias,
                bookingUrl: bookingUrl);

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
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

    private static string BuildCancellationHtml(
        string toName,
        string appName,
        string motivo,
        string barberoName,
        string fechaHora,
        string sugerencias,
        string bookingUrl)
    {
        var safeToName = WebUtility.HtmlEncode(toName);
        var safeAppName = WebUtility.HtmlEncode(appName);
        var safeMotivo = WebUtility.HtmlEncode(motivo);
        var safeBarbero = WebUtility.HtmlEncode(barberoName);
        var safeFecha = WebUtility.HtmlEncode(fechaHora);
        var safeSugerencias = WebUtility.HtmlEncode(sugerencias);
        var safeBookingUrl = WebUtility.HtmlEncode(bookingUrl);

        return $@"<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; background-color: #0a0a0a; color: #ffffff; border: 1px solid #333; border-radius: 16px; overflow: hidden; box-shadow: 0 20px 40px rgba(0,0,0,0.6);"">
  <div style=""height: 6px; background-color: #d8b081;""></div>
  <div style=""background-color: #111; padding: 40px 20px; text-align: center;"">
    <div style=""display: inline-block; padding: 10px 20px; border: 1px solid #d8b081; border-radius: 4px; margin-bottom: 20px;"">
      <span style=""font-size: 24px; font-weight: 900; letter-spacing: 4px; color: #ffffff; text-transform: uppercase;"">MANITO</span>
      <span style=""font-size: 24px; font-weight: 300; letter-spacing: 4px; color: #d8b081; text-transform: uppercase;"">BARBERSHOP</span>
    </div>
    <h1 style=""color: #d8b081; margin: 10px 0 0 0; font-size: 32px; font-weight: 800; text-transform: uppercase; letter-spacing: -1px;"">🚫 Cita Cancelada</h1>
  </div>
  <div style=""padding: 40px 35px; background-color: #0a0a0a;"">
    <p style=""font-size: 18px; color: #ffffff; margin-bottom: 25px;"">Hola <strong>{safeToName}</strong>,</p>
    <p style=""color: #aaa; line-height: 1.8; font-size: 16px; margin-bottom: 30px;"">
      Lamentamos informarte que tu cita en <strong style=""color: #d8b081;"">{safeAppName}</strong> ha sido cancelada. Entendemos que los imprevistos suceden y queremos ayudarte a reprogramar lo antes posible.
    </p>
    <div style=""background-color: #1a1a1a; border-left: 4px solid #d8b081; padding: 25px; margin: 30px 0; border-radius: 8px;"">
      <span style=""color: #d8b081; text-transform: uppercase; font-size: 12px; font-weight: 800; letter-spacing: 2px; display: block; margin-bottom: 10px;"">Motivo informado:</span>
      <p style=""margin: 0; color: #ffffff; font-size: 16px; font-style: italic; line-height: 1.5;"">""{safeMotivo}""</p>
    </div>
    <div style=""margin: 40px 0; border-top: 1px solid #222; padding-top: 30px;"">
      <h3 style=""color: #d8b081; font-size: 14px; text-transform: uppercase; letter-spacing: 2px; margin-bottom: 20px; font-weight: 800;"">Detalles de la sesión original</h3>
      <table style=""width: 100%; border-collapse: collapse;"">
        <tr>
          <td style=""padding: 12px 0; border-bottom: 1px solid #1a1a1a; color: #888; font-size: 14px; width: 40%;"">Barbero:</td>
          <td style=""padding: 12px 0; border-bottom: 1px solid #1a1a1a; color: #fff; font-size: 15px; font-weight: 600;"">{safeBarbero}</td>
        </tr>
        <tr>
          <td style=""padding: 12px 0; border-bottom: 1px solid #1a1a1a; color: #888; font-size: 14px;"">Fecha y Hora:</td>
          <td style=""padding: 12px 0; border-bottom: 1px solid #1a1a1a; color: #fff; font-size: 15px; font-weight: 600;"">{safeFecha}</td>
        </tr>
      </table>
    </div>
    <div style=""background-color: #1a1a1a; padding: 35px 25px; border-radius: 12px; text-align: center; margin-top: 40px; border: 1px solid #222;"">
      <h3 style=""color: #ffffff; margin-bottom: 15px; font-size: 18px; font-weight: 700;"">¿Quieres agendar de nuevo?</h3>
      <p style=""color: #aaa; margin-bottom: 20px; font-size: 14px;"">Te sugerimos estos horarios disponibles:</p>
      <div style=""background-color: #0a0a0a; padding: 15px; border-radius: 8px; border: 1px dashed #d8b081; display: inline-block; min-width: 200px;"">
        <p style=""margin: 0; color: #d8b081; font-weight: 800; font-size: 16px;"">{safeSugerencias}</p>
      </div>
      <div style=""margin-top: 30px;"">
        <a href=""{safeBookingUrl}"" style=""display: inline-block; background-color: #d8b081; color: #000; text-decoration: none; padding: 16px 35px; border-radius: 10px; font-weight: 900; text-transform: uppercase; font-size: 13px; letter-spacing: 1px;"">
          RESERVAR NUEVO TURNO
        </a>
      </div>
    </div>
    <p style=""margin-top: 40px; color: #666; font-size: 13px; text-align: center; line-height: 1.6;"">
      Si tienes alguna duda o prefieres asistencia personalizada, no dudes en contactarnos directamente.
    </p>
  </div>
  <div style=""background-color: #000; padding: 40px 20px; text-align: center; border-top: 1px solid #222;"">
    <p style=""color: #555; font-size: 12px; margin-bottom: 15px; letter-spacing: 1px;"">
      © 2026 <strong style=""color: #777;"">{safeAppName}</strong>. Todos los derechos reservados.
    </p>
    <div style=""color: #444; font-size: 11px;"">
      <p style=""margin: 5px 0;"">Calle 79 #52-12, Barrio El Bosque, Medellín</p>
      <p style=""margin: 5px 0;"">Este es un correo automático, por favor no respondas directamente.</p>
    </div>
  </div>
</div>";
    }
}
