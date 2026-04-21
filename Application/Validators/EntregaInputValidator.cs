using FluentValidation;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

/// <summary>
/// Validador FluentValidation para <see cref="EntregaInput"/>.
/// </summary>
public class EntregaInputValidator : AbstractValidator<EntregaInput>
{
    public EntregaInputValidator()
    {
        RuleFor(x => x.BarberoId)
            .GreaterThan(0).WithMessage("El BarberoId es requerido");

        RuleFor(x => x.UsuarioId)
            .GreaterThan(0).WithMessage("El UsuarioId es requerido");

        RuleFor(x => x.Detalles)
            .NotNull().NotEmpty().WithMessage("La entrega debe tener al menos un detalle");

        RuleForEach(x => x.Detalles).ChildRules(detalle =>
        {
            detalle.RuleFor(d => d.ProductoId)
                .GreaterThan(0).WithMessage("El ProductoId es requerido");
            detalle.RuleFor(d => d.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");
        });
    }
}
