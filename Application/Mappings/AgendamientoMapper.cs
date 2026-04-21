using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Application.Mappings;

/// <summary>
/// Mapeadores manuales estáticos para la entidad Agendamiento.
/// Nombre/Apellido se obtienen navegando a Cliente.Usuario y Barbero.Usuario.
/// </summary>
public static class AgendamientoMapper
{
    public static object ToDto(this Agendamiento a) => new
    {
        a.Id,
        a.ClienteId,
        a.BarberoId,
        a.FechaHora,
        a.Estado,
        a.Duracion,
        a.Precio,
        a.Notas,
        ClienteNombre = a.Cliente?.Usuario != null
            ? $"{a.Cliente.Usuario.Nombre} {a.Cliente.Usuario.Apellido}"
            : string.Empty,
        BarberoNombre = a.Barbero?.Usuario != null
            ? $"{a.Barbero.Usuario.Nombre} {a.Barbero.Usuario.Apellido}"
            : string.Empty
    };
}
