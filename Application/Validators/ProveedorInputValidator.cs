using FluentValidation;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

/// <summary>
/// Validador FluentValidation para <see cref="ProveedorCreateInput"/>.
/// </summary>
public class ProveedorCreateInputValidator : AbstractValidator<ProveedorCreateInput>
{
    public ProveedorCreateInputValidator()
    {
        RuleFor(x => x.TipoProveedor)
            .NotEmpty().WithMessage("El tipo de proveedor es requerido");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.NIT)
            .NotEmpty().WithMessage("El NIT es requerido");

        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo es requerido")
            .EmailAddress().WithMessage("El correo no tiene un formato válido");

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es requerido");

        RuleFor(x => x.Direccion)
            .NotEmpty().WithMessage("La dirección es requerida");
    }
}
