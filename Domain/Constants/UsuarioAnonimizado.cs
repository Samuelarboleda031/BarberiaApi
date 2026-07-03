namespace BarberiaApi.Domain.Constants;

/// <summary>
/// Marcadores de cuentas anonimizadas por baja logica.
/// Cuando un usuario no se puede borrar fisicamente (tiene datos asociados que
/// impiden el DELETE), se anonimiza y su correo pasa a "eliminado_{id}@baja.local".
/// Estas cuentas ya no representan usuarios reales y deben ocultarse de los
/// listados de todos los modulos (Usuarios, Barberos, Clientes, Horarios, etc.).
/// </summary>
public static class UsuarioAnonimizado
{
    /// <summary>Dominio de correo exclusivo de las cuentas dadas de baja logica.</summary>
    public const string DominioBaja = "@baja.local";
}
