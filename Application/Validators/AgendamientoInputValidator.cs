using FluentValidation;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

/// <summary>
/// Validador FluentValidation para <see cref="AgendamientoInput"/>.
/// </summary>
public class AgendamientoInputValidator : AbstractValidator<AgendamientoInput>
{
    public AgendamientoInputValidator()
    {
        RuleFor(x => x.ClienteId)
            .GreaterThan(0).WithMessage("El ClienteId es requerido");

        RuleFor(x => x.BarberoId)
            .GreaterThan(0).WithMessage("El BarberoId es requerido");

        RuleFor(x => x.FechaHora)
            .NotEmpty().WithMessage("La FechaHora del agendamiento es requerida");

        RuleFor(x => x)
            .Must(x => x.ServicioId.HasValue
                     || (x.ServicioIds != null && x.ServicioIds.Any())
                     || x.PaqueteId.HasValue)
            .WithMessage("Debe especificar al menos un servicio o paquete");
    }
}
