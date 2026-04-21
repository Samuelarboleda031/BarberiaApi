using FluentValidation;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

public class UsuarioInputValidator : AbstractValidator<UsuarioInput>
{
    public UsuarioInputValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es requerido");
        RuleFor(x => x.Apellido).NotEmpty().WithMessage("El apellido es requerido");
        RuleFor(x => x.Correo).NotEmpty().EmailAddress().WithMessage("Correo inválido");
        RuleFor(x => x.RolId).GreaterThan(0).WithMessage("El RolId es requerido");
        
        // Contraseña solo requerida al crear (esto se puede manejar con reglas condicionales o herencia)
        // Por ahora lo dejamos como opcional si se está actualizando.
    }
}
