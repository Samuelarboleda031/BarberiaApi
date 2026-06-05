namespace BarberiaApi.Application.Common;

/// <summary>
/// Implementación de IDateTimeProvider que convierte UTC a la zona horaria de Colombia
/// usando TimeZoneInfo en lugar de AddHours(-5) hardcodeado.
/// Registrar como Singleton en DI.
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    // En Windows el ID es "SA Pacific Standard Time"; en Linux/Docker es "America/Bogota"
    private static readonly TimeZoneInfo BogotaTz =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Bogota");

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime NowColombia => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BogotaTz);
}
