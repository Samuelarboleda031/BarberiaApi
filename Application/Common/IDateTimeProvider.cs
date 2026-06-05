namespace BarberiaApi.Application.Common;

/// <summary>
/// Abstracción del reloj del sistema. Centraliza el manejo de fechas/hora
/// para evitar la mezcla de DateTime.Now (timezone del servidor) y
/// DateTime.UtcNow.AddHours(-5) (hardcodeado). Inyectar este servicio
/// en lugar de usar DateTime directamente.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Hora actual en la zona horaria del negocio (America/Bogota, UTC-5).</summary>
    DateTime NowColombia { get; }

    /// <summary>Timestamp UTC puro, para auditoría y comparaciones absolutas.</summary>
    DateTime UtcNow { get; }
}
