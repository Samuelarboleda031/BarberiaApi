using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BarberiaApi.Infrastructure.Services;

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
        && !string.IsNullOrWhiteSpace(_config["WhatsApp:PhoneNumberId"])
        && !string.IsNullOrWhiteSpace(_config["WhatsApp:AccessToken"]);

    public async Task<WhatsAppResult> EnviarTemplateAsync(
        string? telefono,
        string templateName,
        IReadOnlyList<string> parametrosCuerpo,
        CancellationToken ct = default)
    {
        if (!EstaHabilitado)
            return Fail("WhatsApp deshabilitado por configuración.");

        var numero = NormalizarTelefono(telefono);
        if (numero is null)
            return Fail($"Número de teléfono inválido o vacío: '{telefono}'.");

        var phoneNumberId = _config["WhatsApp:PhoneNumberId"]!;
        var accessToken   = _config["WhatsApp:AccessToken"]!;
        var version       = _config["WhatsApp:ApiVersion"] ?? "v20.0";
        var language      = _config["WhatsApp:LanguageCode"] ?? "es";
        var url           = $"https://graph.facebook.com/{version}/{phoneNumberId}/messages";

        var componentes = parametrosCuerpo.Count == 0
            ? Array.Empty<object>()
            : new object[]
            {
                new
                {
                    type       = "body",
                    parameters = parametrosCuerpo
                        .Select(p => new { type = "text", text = p })
                        .ToArray()
                }
            };

        var payload = new
        {
            messaging_product = "whatsapp",
            to                = numero,
            type              = "template",
            template          = new
            {
                name       = templateName,
                language   = new { code = language },
                components = componentes
            }
        };

        try
        {
            ct.ThrowIfCancellationRequested();

            using var http    = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await http.SendAsync(request, ct);
            var body     = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "WhatsApp enviado a {Numero} con template '{Template}'.", numero, templateName);
                return new WhatsAppResult { Enviado = true, Mensaje = "Mensaje WhatsApp enviado correctamente." };
            }

            _logger.LogWarning(
                "WhatsApp API devolvió {Status} para {Numero} (template='{Template}'): {Body}",
                (int)response.StatusCode, numero, templateName, body);
            return Fail($"WhatsApp API error {(int)response.StatusCode}: {body}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al enviar WhatsApp a {Numero}.", numero);
            return Fail(ex.Message);
        }
    }

    // ─── Normaliza a formato E.164 sin el '+' (exigido por la API de Meta)
    // Soporta números colombianos: 10 dígitos que empiezan por 3 → 57XXXXXXXXXX
    private static string? NormalizarTelefono(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return null;

        var digitos = new string(telefono.Where(char.IsDigit).ToArray());

        return digitos.Length switch
        {
            12 when digitos.StartsWith("57") => digitos,       // ya tiene código país
            10 when digitos.StartsWith("3")  => "57" + digitos, // número local colombiano
            _ when digitos.Length > 10       => digitos,        // otro país, usar como viene
            _                                => null
        };
    }

    private static WhatsAppResult Fail(string msg) =>
        new() { Enviado = false, Mensaje = msg };
}
