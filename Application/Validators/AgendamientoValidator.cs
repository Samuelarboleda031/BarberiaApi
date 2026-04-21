using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Validators;

/// <summary>
/// Validaciones de negocio para <see cref="AgendamientoInput"/>.
/// </summary>
public static class AgendamientoValidator
{
    public static (bool isValid, string? error) Validate(AgendamientoInput input)
    {
        if (input.ClienteId <= 0)
            return (false, "El ClienteId es requerido");

        if (input.BarberoId <= 0)
            return (false, "El BarberoId es requerido");

        if (input.FechaHora == default)
            return (false, "La FechaHora del agendamiento es requerida");

        if (input.FechaHora < DateTime.UtcNow.AddMinutes(-5))
            return (false, "No se puede agendar en una fecha pasada");

        bool tieneServicio = input.ServicioId.HasValue
            || (input.ServicioIds != null && input.ServicioIds.Any())
            || input.PaqueteId.HasValue;

        if (!tieneServicio)
            return (false, "Debe especificar al menos un servicio o paquete");

        return (true, null);
    }
}
