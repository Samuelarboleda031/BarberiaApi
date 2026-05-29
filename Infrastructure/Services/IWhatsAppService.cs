namespace BarberiaApi.Infrastructure.Services;

public interface IWhatsAppService
{
    bool EstaHabilitado { get; }

    /// <summary>
    /// Envía un mensaje de plantilla aprobada por Meta.
    /// parametrosCuerpo corresponden a los {{1}}, {{2}}... del body del template.
    /// </summary>
    Task<WhatsAppResult> EnviarTemplateAsync(
        string? telefono,
        string templateName,
        IReadOnlyList<string> parametrosCuerpo,
        CancellationToken ct = default);
}

public sealed class WhatsAppResult
{
    public bool Enviado { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}
