namespace BarberiaApi.Domain.Constants;

/// <summary>
/// Constantes de estado para CreditoBarbero.
/// </summary>
public static class EstadosCredito
{
    public const string Activo                      = "Activo";
    public const string Pagado                      = "Pagado";
    public const string BloqueadoLimite             = "BloqueadoLimite";
    public const string BloqueadoVencimiento        = "BloqueadoVencimiento";
    public const string BloqueadoLimiteYVencimiento = "BloqueadoLimiteYVencimiento";
}
