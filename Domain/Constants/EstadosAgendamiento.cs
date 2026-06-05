namespace BarberiaApi.Domain.Constants;

/// <summary>
/// Constantes de estado para Agendamiento.
/// Usar estas en lugar de strings literales dispersos ("Pendiente", "En Proceso", etc.)
/// para garantizar consistencia y facilitar refactorización.
/// </summary>
public static class EstadosAgendamiento
{
    public const string Pendiente   = "Pendiente";
    public const string Confirmada  = "Confirmada";
    public const string EnProceso   = "En Proceso";
    public const string Completada  = "Completada";
    public const string Cancelada   = "Cancelada";
}
