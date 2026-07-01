using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BarberiaApi.Infrastructure.Services;

/// <summary>
/// Envío de mensajes de WhatsApp a través de la API de Twilio (texto libre).
/// Endpoint: POST https://api.twilio.com/2010-04-01/Accounts/{AccountSid}/Messages.json
/// con autenticación Basic (AccountSid:AuthToken) y cuerpo form-urlencoded.
/// </summary>
public sealed class WhatsAppService : IWhatsAppService
{
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(IConfiguration config, ILogger<WhatsAppService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public bool EstaHabilitado =>
        bool.TryParse(_config["WhatsApp:Habilitado"], out var v) && v
        && !string.IsNullOrWhiteSpace(_config["WhatsApp:Twilio:AccountSid"])
        && !string.IsNullOrWhiteSpace(_config["WhatsApp:Twilio:AuthToken"])
        && !string.IsNullOrWhiteSpace(_config["WhatsApp:Twilio:FromNumber"]);

    public async Task<WhatsAppResult> EnviarTextoAsync(
        string? telefono,
        string mensaje,
        CancellationToken ct = default)
    {
        if (!EstaHabilitado)
            return Fail("WhatsApp deshabilitado por configuración.");

        var numero = NormalizarTelefono(telefono);
        if (numero is null)
            return Fail($"Número de teléfono inválido o vacío: '{telefono}'.");

        var accountSid = _config["WhatsApp:Twilio:AccountSid"]!;
        var authToken  = _config["WhatsApp:Twilio:AuthToken"]!;
        var from       = NormalizarRemitente(_config["WhatsApp:Twilio:FromNumber"]!);
        var url        = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

        var form = new Dictionary<string, string>
        {
            ["From"] = from,
            ["To"]   = $"whatsapp:+{numero}",
            ["Body"] = mensaje
        };

        try
        {
            ct.ThrowIfCancellationRequested();

            using var http    = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            var credenciales = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credenciales);
            request.Content = new FormUrlEncodedContent(form);

            var response = await http.SendAsync(request, ct);
            var body     = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp enviado a {Numero} vía Twilio (remitente '{From}').", numero, from);
                return new WhatsAppResult { Enviado = true, Mensaje = "Mensaje WhatsApp enviado correctamente." };
            }

            _logger.LogWarning(
                "Twilio devolvió {Status} para {Numero} (remitente='{From}'): {Body}",
                (int)response.StatusCode, numero, from, body);
            return Fail($"Twilio error {(int)response.StatusCode}: {body}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al enviar WhatsApp a {Numero} vía Twilio.", numero);
            return Fail(ex.Message);
        }
    }

    // ─── Normaliza a formato internacional sin el '+' (E.164 sin signo).
    // Si el número no trae el código de país de Colombia (57), se lo antepone.
    private static string? NormalizarTelefono(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return null;

        var digitos = new string(telefono.Where(char.IsDigit).ToArray());
        if (digitos.Length == 0) return null;

        // Ya trae el código de país de Colombia → usar tal cual.
        if (digitos.StartsWith("57") && digitos.Length >= 12) return digitos;

        // Cualquier otro caso (ej. 10 dígitos locales) → anteponer 57.
        return "57" + digitos;
    }

    // ─── El remitente de Twilio debe llevar el prefijo 'whatsapp:'.
    // Acepta tanto "whatsapp:+14155238886" como "+14155238886" o "14155238886".
    private static string NormalizarRemitente(string from)
    {
        var valor = from.Trim();
        if (valor.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
            return valor;
        if (!valor.StartsWith('+'))
            valor = "+" + valor;
        return $"whatsapp:{valor}";
    }

    private static WhatsAppResult Fail(string msg) =>
        new() { Enviado = false, Mensaje = msg };
}
