using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Application.Mappings;

/// <summary>
/// Mapeadores manuales estáticos para la entidad Cliente.
/// Nombre/Apellido/Correo vienen del Usuario relacionado.
/// </summary>
public static class ClienteMapper
{
    public static object ToDto(this Cliente c) => new
    {
        c.Id,
        c.UsuarioId,
        Nombre = c.Usuario?.Nombre ?? string.Empty,
        Apellido = c.Usuario?.Apellido ?? string.Empty,
        Correo = c.Usuario?.Correo ?? string.Empty,
        c.Telefono,
        c.Estado,
        c.FechaRegistro
    };
}
