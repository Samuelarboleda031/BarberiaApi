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
    // Soporta números colombianos: 10 dígitos que empiezan por 3 → 57XXXXXXXXXX
    private static string? NormalizarTelefono(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return null;

        var digitos = new string(telefono.Where(char.IsDigit).ToArray());

        return digitos.Length switch
        {
            12 when digitos.StartsWith("57") => digitos,        // ya tiene código país
            10 when digitos.StartsWith("3")  => "57" + digitos, // número local colombiano
            _ when digitos.Length > 10       => digitos,        // otro país, usar como viene
            _                                => null
        };
    }

    private static WhatsAppResult Fail(string msg) =>
        new() { Enviado = false, Mensaje = msg };
}
