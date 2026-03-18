using BarberiaApi.Models;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace BarberiaApi.Services;

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
        string? GetSetting(string key, string envKey)
        {
            var value = _configuration[key];
            if (!string.IsNullOrWhiteSpace(value)) return value;
            return Environment.GetEnvironmentVariable(envKey);
        }

        var correoHabilitadoConfig = _configuration.GetValue<bool?>("Notificaciones:Correo:Habilitado");
        var correoHabilitadoEnv = Environment.GetEnvironmentVariable("NOTIFICACIONES_CORREO_HABILITADO")
            ?? Environment.GetEnvironmentVariable("SMTP_ENABLED");
        var correoHabilitado = correoHabilitadoConfig
            ?? (bool.TryParse(correoHabilitadoEnv, out var habilitadoEnv) && habilitadoEnv);

        if (!correoHabilitado)
        {
            return new ResultadoNotificacionCita
            {
                Enviado = false,
                Canal = "correo_smtp",
                Mensaje = "El envío de correo está deshabilitado. Activa Notificaciones:Correo:Habilitado o SMTP_ENABLED=true."
            };
        }

        var destinatario = agendamiento.Cliente?.Usuario?.Correo?.Trim();
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            return new ResultadoNotificacionCita
            {
                Enviado = false,
                Canal = "correo_smtp",
                Mensaje = "No se encontró correo del cliente para enviar notificación."
            };
        }

        var host = GetSetting("Notificaciones:Correo:Host", "SMTP_HOST")?.Trim();
        var puertoConfig = _configuration.GetValue<int?>("Notificaciones:Correo:Puerto");
        var puertoEnv = Environment.GetEnvironmentVariable("SMTP_PORT");
        var puerto = puertoConfig ?? (int.TryParse(puertoEnv, out var p) ? p : 587);
        var useSslConfig = _configuration.GetValue<bool?>("Notificaciones:Correo:UseSsl");
        var useSslEnv = Environment.GetEnvironmentVariable("SMTP_SSL");
        var useSsl = useSslConfig ?? (bool.TryParse(useSslEnv, out var ssl) ? ssl : true);
        var usuario = GetSetting("Notificaciones:Correo:Usuario", "SMTP_USER")?.Trim();
        var contrasena = GetSetting("Notificaciones:Correo:Contrasena", "SMTP_PASSWORD");
        var remitente = GetSetting("Notificaciones:Correo:Remitente", "SMTP_FROM")?.Trim();
        var nombreRemitente = GetSetting("Notificaciones:Correo:NombreRemitente", "SMTP_FROM_NAME")?.Trim();

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(remitente))
        {
            return new ResultadoNotificacionCita
            {
                Enviado = false,
                Canal = "correo_smtp",
                Mensaje = "Configuración SMTP incompleta: Host y Remitente son requeridos."
            };
        }

        var sb = new StringBuilder();
        sb.Append("<h2>Cancelación de cita</h2>");
        sb.Append("<p>Tu cita fue cancelada por desactivación de horario.</p>");
        sb.Append($"<p><strong>Motivo:</strong> {WebUtility.HtmlEncode(motivo)}</p>");
        sb.Append($"<p><strong>Fecha original:</strong> {agendamiento.FechaHora:yyyy-MM-dd HH:mm}</p>");
        if (sugerenciasReprogramacion.Any())
        {
            sb.Append("<p><strong>Opciones sugeridas para reprogramar:</strong></p><ul>");
            foreach (var sugerencia in sugerenciasReprogramacion)
            {
                sb.Append($"<li>{sugerencia:yyyy-MM-dd HH:mm}</li>");
            }
            sb.Append("</ul>");
        }
        sb.Append("<p>Por favor ingresa al sistema para elegir una nueva fecha.</p>");

        try
        {
            using var smtp = new SmtpClient(host, puerto)
            {
                EnableSsl = useSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                smtp.Credentials = new NetworkCredential(usuario, contrasena ?? string.Empty);
            }

            using var mail = new MailMessage
            {
                From = new MailAddress(remitente, string.IsNullOrWhiteSpace(nombreRemitente) ? "Barbería" : nombreRemitente),
                Subject = "Cancelación y reprogramación de cita",
                Body = sb.ToString(),
                IsBodyHtml = true
            };
            mail.To.Add(destinatario);

            await smtp.SendMailAsync(mail);
            return new ResultadoNotificacionCita
            {
                Enviado = true,
                Canal = "correo_smtp",
                Mensaje = "Correo enviado correctamente al cliente."
            };
        }
        catch (Exception ex)
        {
            return new ResultadoNotificacionCita
            {
                Enviado = false,
                Canal = "correo_smtp",
                Mensaje = $"No se pudo enviar correo SMTP: {ex.Message}"
            };
        }
    }
}
