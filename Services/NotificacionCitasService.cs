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
        var correoHabilitado = _configuration.GetValue<bool>("Notificaciones:Correo:Habilitado", false);
        if (!correoHabilitado)
        {
            return new ResultadoNotificacionCita
            {
                Enviado = true,
                Canal = "in_app",
                Mensaje = "Cancelación notificada para consumo frontend (in-app), sin envío de correo."
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

        var host = _configuration["Notificaciones:Correo:Host"]?.Trim();
        var puerto = _configuration.GetValue<int?>("Notificaciones:Correo:Puerto") ?? 587;
        var useSsl = _configuration.GetValue<bool>("Notificaciones:Correo:UseSsl", true);
        var usuario = _configuration["Notificaciones:Correo:Usuario"]?.Trim();
        var contrasena = _configuration["Notificaciones:Correo:Contrasena"];
        var remitente = _configuration["Notificaciones:Correo:Remitente"]?.Trim();
        var nombreRemitente = _configuration["Notificaciones:Correo:NombreRemitente"]?.Trim();

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
