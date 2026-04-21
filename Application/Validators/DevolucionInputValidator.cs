using FluentValidation;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

/// <summary>
/// Validador FluentValidation para <see cref="DevolucionInput"/>.
/// </summary>
public class DevolucionInputValidator : AbstractValidator<DevolucionInput>
{
    public DevolucionInputValidator()
    {
        RuleFor(x => x.UsuarioId)
            .GreaterThan(0).WithMessage("El UsuarioId es requerido");

        RuleFor(x => x.ProductoId)
            .GreaterThan(0).WithMessage("El ProductoId es requerido");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");

        RuleFor(x => x.MontoDevuelto)
            .GreaterThanOrEqualTo(0).WithMessage("El monto devuelto no puede ser negativo");

        RuleFor(x => x)
            .Must(x => x.VentaId.HasValue || x.EntregaId.HasValue)
            .WithMessage("Debe especificar un VentaId o un EntregaId");
    }
}

/// <summary>
/// Validador FluentValidation para <see cref="DevolucionBatchInput"/>.
/// </summary>
public class DevolucionBatchInputValidator : AbstractValidator<DevolucionBatchInput>
{
    public DevolucionBatchInputValidator()
    {
        RuleFor(x => x.VentaId)
            .GreaterThan(0).WithMessage("El VentaId es requerido");

        RuleFor(x => x.UsuarioId)
            .GreaterThan(0).WithMessage("El UsuarioId es requerido");

        RuleFor(x => x.MotivoCategoria)
            .NotEmpty().WithMessage("La categoría del motivo es requerida");

        RuleFor(x => x.Items)
            .NotNull().NotEmpty().WithMessage("Debe incluir al menos un item de devolución");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductoId)
                .GreaterThan(0).WithMessage("El ProductoId es requerido en cada item");
            item.RuleFor(i => i.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");
            item.RuleFor(i => i.MontoDevuelto)
                .GreaterThanOrEqualTo(0).WithMessage("El monto devuelto no puede ser negativo");
        });
    }
}
