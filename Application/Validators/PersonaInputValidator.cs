using FluentValidation;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

public class ClienteInputValidator : AbstractValidator<ClienteInput>
{
    public ClienteInputValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es requerido");
        RuleFor(x => x.Apellido).NotEmpty().WithMessage("El apellido es requerido");
        RuleFor(x => x.Documento).NotEmpty().WithMessage("El documento es requerido");
        RuleFor(x => x.Correo).NotEmpty().EmailAddress().WithMessage("Correo inválido");
        RuleFor(x => x.UsuarioId).GreaterThan(0).WithMessage("El UsuarioId es requerido");
    }
}

public class BarberoInputValidator : AbstractValidator<BarberoInput>
{
    public BarberoInputValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es requerido");
        RuleFor(x => x.Apellido).NotEmpty().WithMessage("El apellido es requerido");
        RuleFor(x => x.Documento).NotEmpty().WithMessage("El documento es requerido");
        RuleFor(x => x.Correo).NotEmpty().EmailAddress().WithMessage("Correo inválido");
        RuleFor(x => x.UsuarioId).GreaterThan(0).WithMessage("El UsuarioId es requerido");
    }
}
