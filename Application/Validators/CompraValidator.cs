using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

/// <summary>
/// Validaciones de negocio para <see cref="CompraInput"/>.
/// </summary>
public static class CompraValidator
{
    public static (bool isValid, string? error) Validate(CompraInput input)
    {
        if (input.ProveedorId <= 0)
            return (false, "El ProveedorId es requerido");

        if (input.UsuarioId <= 0)
            return (false, "El UsuarioId es requerido");

        if (input.Detalles == null || !input.Detalles.Any())
            return (false, "La compra debe tener al menos un detalle");

        if (input.Detalles.Any(d => d.Cantidad <= 0))
            return (false, "La cantidad de cada detalle debe ser mayor a 0");

        if (input.Detalles.Any(d => d.PrecioUnitario < 0))
            return (false, "El precio unitario no puede ser negativo");

        return (true, null);
    }
}
