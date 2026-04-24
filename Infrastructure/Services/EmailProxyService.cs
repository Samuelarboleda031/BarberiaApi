using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BarberiaApi.Infrastructure.Services;

public sealed class EmailProxyService : IEmailProxyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmailProxyService> _logger;
    private readonly IConfiguration _configuration;

    public EmailProxyService(
        HttpClient httpClient,
        ILogger<EmailProxyService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ProxyEmailResult> EnviarCancelacionAsync(
        CancelacionEmailProxyRequest request,
        CancellationToken cancellationToken = default)
    {
        var serviceId = _configuration["EmailJs:ServiceId"];
        var templateId = _configuration["EmailJs:TemplateIdCancelacion"];
        var publicKey = _configuration["EmailJs:PublicKey"];
        var privateKey = _configuration["EmailJs:PrivateKey"];
        var endpoint = _configuration["EmailJs:Endpoint"] ?? "https://api.emailjs.com/api/v1.0/email/send";

        if (string.IsNullOrWhiteSpace(serviceId) ||
            string.IsNullOrWhiteSpace(templateId) ||
            string.IsNullOrWhiteSpace(publicKey))
        {
            return new ProxyEmailResult
            {
                Enviado = false,
                CodigoRespuesta = 500,
                Mensaje = "Configuración EmailJs incompleta en backend."
            };
        }

        var templateParams = new
        {
            to_name = request.ClienteNombre,
            to_email = request.ClienteEmail,
            barbero_name = request.BarberoNombre,
            fecha_hora = FormatFecha(request.FechaOriginal),
            motivo = request.Motivo,
            sugerencias = request.SugerenciasReprogramacion is { Count: > 0 }
                ? string.Join(" | ", request.SugerenciasReprogramacion.Select(FormatFecha))
                : "No disponibles",
            app_name = string.IsNullOrWhiteSpace(request.AppName) ? "Barbería App" : request.AppName
        };

        var payload = new Dictionary<string, object?>
        {
            ["service_id"] = serviceId,
            ["template_id"] = templateId,
            ["user_id"] = publicKey,
            ["template_params"] = templateParams
        };
        if (!string.IsNullOrWhiteSpace(privateKey))
            payload["accessToken"] = privateKey;

        using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new ProxyEmailResult
            {
                Enviado = true,
                CodigoRespuesta = (int)response.StatusCode,
                Mensaje = "Correo enviado vía EmailJS desde backend."
            };
        }

        _logger.LogWarning("EmailJS proxy falló. Status: {Status}. Body: {Body}", (int)response.StatusCode, raw);
        return new ProxyEmailResult
        {
            Enviado = false,
            CodigoRespuesta = (int)response.StatusCode,
            Mensaje = $"EmailJS rechazó la solicitud: {raw}"
        };
    }

    private static string FormatFecha(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        if (!DateTime.TryParse(input, out var dt))
            return input;

        return dt.ToString("dddd, d 'de' MMMM, hh:mm tt", new CultureInfo("es-ES"));
    }
}
