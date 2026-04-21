using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

/// <summary>
/// Validaciones de negocio para <see cref="VentaInput"/>.
/// Retorna una tupla (isValid, errorMessage) sin dependencias externas.
/// </summary>
public static class VentaValidator
{
    public static (bool isValid, string? error) Validate(VentaInput input)
    {
        if (input.Detalles == null || !input.Detalles.Any())
            return (false, "La venta debe tener al menos un detalle");

        if (input.Detalles.Any(d => d.Cantidad <= 0))
            return (false, "La cantidad de cada detalle debe ser mayor a 0");

        if (input.Detalles.Any(d => d.PrecioUnitario < 0))
            return (false, "El precio unitario no puede ser negativo");

        return (true, null);
    }
}
