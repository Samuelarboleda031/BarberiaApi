namespace BarberiaApi.Infrastructure.Services;

public interface IEmailProxyService
{
    Task<ProxyEmailResult> EnviarCancelacionAsync(
        CancelacionEmailProxyRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class CancelacionEmailProxyRequest
{
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;
    public string BarberoNombre { get; set; } = string.Empty;
    public string FechaOriginal { get; set; } = string.Empty;
    public string Motivo { get; set; } = "Cita cancelada por el administrador/barbero.";
    public List<string> SugerenciasReprogramacion { get; set; } = new();
    public string AppName { get; set; } = "Barbería App";
}

public sealed class ProxyEmailResult
{
    public bool Enviado { get; set; }
    public int CodigoRespuesta { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
