using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Application.Mappings;

/// <summary>
/// Mapeadores manuales estáticos para la entidad Venta.
/// Cliente.Nombre/Apellido no existen; se navega a Cliente.Usuario.
/// </summary>
public static class VentaMapper
{
    public static object ToDto(this Venta v) => new
    {
        v.Id,
        v.Fecha,
        v.Total,
        v.Estado,
        ClienteNombre = v.Cliente?.Usuario != null
            ? $"{v.Cliente.Usuario.Nombre} {v.Cliente.Usuario.Apellido}"
            : v.ClienteNombre
    };
}
