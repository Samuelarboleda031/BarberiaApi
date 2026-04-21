using FluentValidation;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

/// <summary>
/// Validador FluentValidation para <see cref="CompraInput"/>.
/// </summary>
public class CompraInputValidator : AbstractValidator<CompraInput>
{
    public CompraInputValidator()
    {
        RuleFor(x => x.ProveedorId)
            .GreaterThan(0).WithMessage("El ProveedorId es requerido");

        RuleFor(x => x.UsuarioId)
            .GreaterThan(0).WithMessage("El UsuarioId es requerido");

        RuleFor(x => x.Detalles)
            .NotNull().WithMessage("Los detalles de la compra son requeridos")
            .NotEmpty().WithMessage("La compra debe tener al menos un detalle");

        RuleForEach(x => x.Detalles).ChildRules(detalle =>
        {
            detalle.RuleFor(d => d.ProductoId)
                .GreaterThan(0).WithMessage("El ProductoId es requerido en cada detalle");

            detalle.RuleFor(d => d.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad de cada detalle debe ser mayor a 0");

            detalle.RuleFor(d => d.PrecioUnitario)
                .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo");
        });
    }
}
