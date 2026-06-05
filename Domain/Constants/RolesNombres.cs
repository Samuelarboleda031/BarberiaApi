namespace BarberiaApi.Domain.Constants;

/// <summary>
/// Constantes canónicas de nombres de roles del sistema.
/// Usar estas constantes en lugar de IDs numéricos (RolId == 2, etc.)
/// para evitar que cambios de orden en la BD rompan la lógica.
/// </summary>
public static class RolesNombres
{
    public const string SuperAdmin = "super_admin";
    public const string Admin = "admin";
    public const string Barbero = "barbero";
    public const string Cliente = "cliente";
}
