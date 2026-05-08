using FluentValidation;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

/// <summary>
/// Validador FluentValidation para <see cref="ProveedorCreateInput"/>.
/// Aplica reglas condicionales según TipoProveedor (Natural / Juridico).
/// </summary>
public class ProveedorCreateInputValidator : AbstractValidator<ProveedorCreateInput>
{
    public ProveedorCreateInputValidator()
    {
        // Reglas comunes: aplican a ambos tipos
        RuleFor(x => x.TipoProveedor)
            .NotEmpty().WithMessage("El tipo de proveedor es requerido")
            .Must(t => t == "Natural" || t == "Juridico")
            .WithMessage("TipoProveedor debe ser 'Natural' o 'Juridico'");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres");

        RuleFor(x => x.Identificacion)
            .NotEmpty().WithMessage("La identificación es requerida");

        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo es requerido")
            .EmailAddress().WithMessage("El correo no tiene un formato válido");

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es requerido");

        // Reglas exclusivas para Jurídico: todos los campos son obligatorios
        When(x => x.TipoProveedor == "Juridico", () =>
        {
            RuleFor(x => x.Direccion)
                .NotEmpty().WithMessage("La dirección es requerida para proveedores jurídicos");

            RuleFor(x => x.Ciudad)
                .NotEmpty().WithMessage("La ciudad es requerida para proveedores jurídicos");

            RuleFor(x => x.Departamento)
                .NotEmpty().WithMessage("El departamento es requerido para proveedores jurídicos");

            RuleFor(x => x.RepresentanteLegal)
                .NotEmpty().WithMessage("El representante legal es requerido para proveedores jurídicos");

            RuleFor(x => x.IdentificacionRepresentante)
                .NotEmpty().WithMessage("La identificación del representante es requerida para proveedores jurídicos");

            RuleFor(x => x.CorreoRepresentante)
                .NotEmpty().WithMessage("El correo del representante es requerido para proveedores jurídicos")
                .EmailAddress().WithMessage("El correo del representante no tiene un formato válido");

            RuleFor(x => x.TelefonoRepresentante)
                .NotEmpty().WithMessage("El teléfono del representante es requerido para proveedores jurídicos");
        });
    }
}
