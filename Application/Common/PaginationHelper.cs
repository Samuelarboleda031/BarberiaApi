namespace BarberiaApi.Application.Common;

/// <summary>
/// Centraliza la validación y sanitización de parámetros de paginación.
/// Evita que un cliente pida pageSize arbitrariamente grande.
/// </summary>
public static class PaginationHelper
{
    /// <summary>Tope global para la mayoría de listados.</summary>
    public const int DefaultMaxPageSize = 100;

    /// <summary>
    /// Sanitiza page y pageSize en una sola llamada.
    /// Garantiza page >= 1 y 1 <= pageSize <= maxPageSize.
    /// </summary>
    public static void Sanitize(ref int page, ref int pageSize, int maxPageSize = DefaultMaxPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > maxPageSize) pageSize = maxPageSize;
    }

    /// <summary>
    /// Calcula el número total de páginas.
    /// Evita división por cero si pageSize es 0 (aunque Sanitize lo previene).
    /// </summary>
    public static int TotalPages(int totalCount, int pageSize) =>
        pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
}
