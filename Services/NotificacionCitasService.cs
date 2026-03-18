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

    private static string? NormalizeSetting(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        var hasWrappingDoubleQuotes = normalized.Length >= 2 && normalized.StartsWith("\"") && normalized.EndsWith("\"");
        var hasWrappingSingleQuotes = normalized.Length >= 2 && normalized.StartsWith("'") && normalized.EndsWith("'");
        var hasWrappingBackticks = normalized.Length >= 2 && normalized.StartsWith("`") && normalized.EndsWith("`");
        if (hasWrappingDoubleQuotes || hasWrappingSingleQuotes || hasWrappingBackticks)
        {
            normalized = normalized.Substring(1, normalized.Length - 2).Trim();
        }
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private string? GetSetting(string key, string envKey)
    {
        var value = NormalizeSetting(_configuration[key]);
        if (!string.IsNullOrWhiteSpace(value)) return value;
        return NormalizeSetting(Environment.GetEnvironmentVariable(envKey));
    }

    private async Task<ResultadoNotificacionCita> EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        var correoHabilitadoConfig = _configuration.GetValue<bool?>("Notificaciones:Correo:Habilitado");
        if (!correoHabilitadoConfig.HasValue)
        {
            var correoHabilitadoConfigRaw = NormalizeSetting(_configuration["Notificaciones:Correo:Habilitado"]);
            if (bool.TryParse(correoHabilitadoConfigRaw, out var correoHabilitadoConfigParsed))
            {
                correoHabilitadoConfig = correoHabilitadoConfigParsed;
            }
        }
        var correoHabilitadoEnv = NormalizeSetting(
            Environment.GetEnvironmentVariable("NOTIFICACIONES_CORREO_HABILITADO")
            ?? Environment.GetEnvironmentVariable("SMTP_ENABLED"));
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
        if (!puertoConfig.HasValue)
        {
            var puertoConfigRaw = NormalizeSetting(_configuration["Notificaciones:Correo:Puerto"]);
            if (int.TryParse(puertoConfigRaw, out var puertoConfigParsed))
            {
                puertoConfig = puertoConfigParsed;
            }
        }
        var puertoEnv = NormalizeSetting(Environment.GetEnvironmentVariable("SMTP_PORT"));
        var puerto = puertoConfig ?? (int.TryParse(puertoEnv, out var p) ? p : 587);
        var timeoutSegundosConfig = _configuration.GetValue<int?>("Notificaciones:Correo:TimeoutSegundos");
        if (!timeoutSegundosConfig.HasValue)
        {
            var timeoutSegundosConfigRaw = NormalizeSetting(_configuration["Notificaciones:Correo:TimeoutSegundos"]);
            if (int.TryParse(timeoutSegundosConfigRaw, out var timeoutSegundosConfigParsed))
            {
                timeoutSegundosConfig = timeoutSegundosConfigParsed;
            }
        }
        var timeoutSegundosEnv = NormalizeSetting(Environment.GetEnvironmentVariable("SMTP_TIMEOUT_SECONDS"));
        var timeoutSegundos = timeoutSegundosConfig ?? (int.TryParse(timeoutSegundosEnv, out var timeoutEnvParsed) ? timeoutEnvParsed : 6);
        if (timeoutSegundos < 2) timeoutSegundos = 2;
        if (timeoutSegundos > 30) timeoutSegundos = 30;
        var useSslConfig = _configuration.GetValue<bool?>("Notificaciones:Correo:UseSsl");
        if (!useSslConfig.HasValue)
        {
            var useSslConfigRaw = NormalizeSetting(_configuration["Notificaciones:Correo:UseSsl"]);
            if (bool.TryParse(useSslConfigRaw, out var useSslConfigParsed))
            {
                useSslConfig = useSslConfigParsed;
            }
        }
        var useSslEnv = NormalizeSetting(Environment.GetEnvironmentVariable("SMTP_SSL"));
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

        try
        {
            using var smtp = new SmtpClient(host, puerto)
            {
                EnableSsl = useSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = timeoutSegundos * 1000
            };

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                smtp.Credentials = new NetworkCredential(usuario, contrasena ?? string.Empty);
            }

            using var mail = new MailMessage
            {
                From = new MailAddress(remitente, string.IsNullOrWhiteSpace(nombreRemitente) ? "Barbería" : nombreRemitente),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };
            mail.To.Add(destinatario);

            await smtp.SendMailAsync(mail).WaitAsync(TimeSpan.FromSeconds(timeoutSegundos));
            return new ResultadoNotificacionCita
            {
                Enviado = true,
                Canal = "correo_smtp",
                Mensaje = "Correo enviado correctamente al cliente."
            };
        }
        catch (TimeoutException)
        {
            return new ResultadoNotificacionCita
            {
                Enviado = false,
                Canal = "correo_smtp",
                Mensaje = $"Timeout SMTP tras {timeoutSegundos}s. El correo no respondió a tiempo."
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

    public async Task<ResultadoNotificacionCita> NotificarCancelacionPorDesactivacionAsync(
        Agendamiento agendamiento,
        string motivo,
        IReadOnlyCollection<DateTime> sugerenciasReprogramacion)
    {
        var destinatario = agendamiento.Cliente?.Usuario?.Correo?.Trim();
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            return new ResultadoNotificacionCita
            {
                Enviado = false,
                Canal = "correo_smtp",
                Mensaje = "No se encontró correo del cliente."
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

        return await EnviarCorreoAsync(destinatario, "Cancelación y reprogramación de cita", sb.ToString());
    }

    public async Task<ResultadoNotificacionCita> NotificarCancelacionGeneralAsync(
        Agendamiento agendamiento,
        string motivo)
    {
        var destinatario = agendamiento.Cliente?.Usuario?.Correo?.Trim();
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            return new ResultadoNotificacionCita
            {
                Enviado = false,
                Canal = "correo_smtp",
                Mensaje = "No se encontró correo del cliente."
            };
        }

        var barberoNombre = agendamiento.Barbero?.Usuario != null 
            ? $"{agendamiento.Barbero.Usuario.Nombre} {agendamiento.Barbero.Usuario.Apellido}" 
            : "Barbero";
        
        var servicioNombre = agendamiento.Servicio?.Nombre ?? agendamiento.Paquete?.Nombre ?? "Servicio";

        var sb = new StringBuilder();
        sb.Append("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>");
        sb.Append("<h2 style='color: #d9534f;'>Notificación de Cancelación de Cita</h2>");
        sb.Append("<p>Hola,</p>");
        sb.Append("<p>Te informamos que tu cita ha sido cancelada.</p>");
        sb.Append("<div style='background-color: #f9f9f9; padding: 15px; border-radius: 5px; margin: 20px 0;'>");
        sb.Append($"<p><strong>Servicio:</strong> {WebUtility.HtmlEncode(servicioNombre)}</p>");
        sb.Append($"<p><strong>Barbero:</strong> {WebUtility.HtmlEncode(barberoNombre)}</p>");
        sb.Append($"<p><strong>Fecha y Hora:</strong> {agendamiento.FechaHora:dd/MM/yyyy HH:mm}</p>");
        sb.Append("</div>");
        if (!string.IsNullOrWhiteSpace(motivo))
        {
            sb.Append($"<p><strong>Motivo de cancelación:</strong> {WebUtility.HtmlEncode(motivo)}</p>");
        }
        sb.Append("<p>Si deseas agendar una nueva cita, por favor visita nuestra plataforma.</p>");
        sb.Append("<p>Atentamente,<br><strong>Manito Barbershop</strong></p>");
        sb.Append("</div>");

        return await EnviarCorreoAsync(destinatario, "Cancelación de Cita - Manito Barbershop", sb.ToString());
    }
}
