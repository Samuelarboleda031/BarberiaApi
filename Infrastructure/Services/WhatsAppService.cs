using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BarberiaApi.Infrastructure.Services;

/// <summary>
/// Envío de mensajes de WhatsApp a través de Evolution API (texto libre, sin plantillas).
/// Endpoint: POST {BaseUrl}/message/sendText/{Instance} con header apikey.
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
        && !string.IsNullOrWhiteSpace(_config["WhatsApp:Evolution:BaseUrl"])
        && !string.IsNullOrWhiteSpace(_config["WhatsApp:Evolution:Instance"])
        && !string.IsNullOrWhiteSpace(_config["WhatsApp:Evolution:ApiKey"]);

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

        var baseUrl  = _config["WhatsApp:Evolution:BaseUrl"]!.TrimEnd('/');
        var instance = _config["WhatsApp:Evolution:Instance"]!;
        var apiKey   = _config["WhatsApp:Evolution:ApiKey"]!;
        var url      = $"{baseUrl}/message/sendText/{instance}";

        var payload = new
        {
            number = numero,
            text   = mensaje
        };

        try
        {
            ct.ThrowIfCancellationRequested();

            using var http    = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await http.SendAsync(request, ct);
            var body     = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp enviado a {Numero} vía Evolution (instancia '{Instance}').", numero, instance);
                return new WhatsAppResult { Enviado = true, Mensaje = "Mensaje WhatsApp enviado correctamente." };
            }

            _logger.LogWarning(
                "Evolution API devolvió {Status} para {Numero} (instancia='{Instance}'): {Body}",
                (int)response.StatusCode, numero, instance, body);
            return Fail($"Evolution API error {(int)response.StatusCode}: {body}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al enviar WhatsApp a {Numero} vía Evolution.", numero);
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

    private static WhatsAppResult Fail(string msg) =>
        new() { Enviado = false, Mensaje = msg };
}
