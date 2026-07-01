namespace BarberiaApi.Infrastructure.Services;

public interface IWhatsAppService
{
    bool EstaHabilitado { get; }

    /// <summary>
    /// Envía un mensaje de texto libre por WhatsApp a través de la API de Twilio.
    /// </summary>
    Task<WhatsAppResult> EnviarTextoAsync(
        string? telefono,
        string mensaje,
        CancellationToken ct = default);
}

public sealed class WhatsAppResult
{
    public bool Enviado { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}
