using FluentValidation;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

/// <summary>
/// Validador FluentValidation para <see cref="VentaInput"/>.
/// Se ejecuta automáticamente antes de que el controlador procese la request.
/// Solo valida estructura del input; las reglas de negocio (stock, existencia) se mantienen en el Service.
/// </summary>
public class VentaInputValidator : AbstractValidator<VentaInput>
{
    public VentaInputValidator()
    {
        RuleFor(x => x.Detalles)
            .NotNull().WithMessage("Los detalles de la venta son requeridos")
            .NotEmpty().WithMessage("La venta debe tener al menos un detalle");

        RuleForEach(x => x.Detalles).ChildRules(detalle =>
        {
            detalle.RuleFor(d => d.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad de cada detalle debe ser mayor a 0");

            detalle.RuleFor(d => d.PrecioUnitario)
                .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo");

            detalle.RuleFor(d => d)
                .Must(d => d.ProductoId.HasValue || d.ServicioId.HasValue || d.PaqueteId.HasValue)
                .WithMessage("Cada detalle debe tener un ProductoId, ServicioId o PaqueteId");
        });

        RuleFor(x => x.Descuento)
            .GreaterThanOrEqualTo(0).When(x => x.Descuento.HasValue)
            .WithMessage("El descuento no puede ser negativo");
    }
}

/// <summary>
/// Validador para <see cref="DetalleVentaInput"/> cuando se usa de forma independiente.
/// </summary>
public class DetalleVentaInputValidator : AbstractValidator<DetalleVentaInput>
{
    public DetalleVentaInputValidator()
    {
        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");

        RuleFor(x => x.PrecioUnitario)
            .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo");
    }
}
