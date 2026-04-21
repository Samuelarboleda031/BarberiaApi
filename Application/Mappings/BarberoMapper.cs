using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Application.Mappings;

/// <summary>
/// Mapeadores manuales estáticos para la entidad Barbero.
/// Nombre/Apellido/Correo vienen del Usuario relacionado.
/// </summary>
public static class BarberoMapper
{
    public static object ToDto(this Barbero b) => new
    {
        b.Id,
        b.UsuarioId,
        Nombre = b.Usuario?.Nombre ?? string.Empty,
        Apellido = b.Usuario?.Apellido ?? string.Empty,
        Correo = b.Usuario?.Correo ?? string.Empty,
        b.Telefono,
        b.Especialidad,
        b.Estado,
        b.FechaContratacion
    };
}
