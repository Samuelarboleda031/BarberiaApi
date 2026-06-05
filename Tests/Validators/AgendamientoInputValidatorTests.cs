using FluentAssertions;
using FluentValidation.TestHelper;
using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Validators;

namespace BarberiaApi.Tests.Validators;

public class AgendamientoInputValidatorTests
{
    private readonly AgendamientoInputValidator _validator = new();

    [Fact]
    public void Agendamiento_valido_con_servicio_no_tiene_errores()
    {
        var input = new AgendamientoInput
        {
            ClienteId = 1,
            BarberoId = 2,
            ServicioId = 3,
            FechaHora = DateTime.UtcNow.AddDays(1)
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Agendamiento_valido_con_paquete_no_tiene_errores()
    {
        var input = new AgendamientoInput
        {
            ClienteId = 1,
            BarberoId = 2,
            PaqueteId = 5,
            FechaHora = DateTime.UtcNow.AddDays(1)
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Agendamiento_valido_con_servicioIds_no_tiene_errores()
    {
        var input = new AgendamientoInput
        {
            ClienteId = 1,
            BarberoId = 2,
            ServicioIds = new List<int> { 1, 2 },
            FechaHora = DateTime.UtcNow.AddDays(1)
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ClienteId_cero_genera_error()
    {
        var input = new AgendamientoInput
        {
            ClienteId = 0,
            BarberoId = 2,
            ServicioId = 3,
            FechaHora = DateTime.UtcNow.AddDays(1)
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.ClienteId)
            .WithErrorMessage("El ClienteId es requerido");
    }

    [Fact]
    public void BarberoId_cero_genera_error()
    {
        var input = new AgendamientoInput
        {
            ClienteId = 1,
            BarberoId = 0,
            ServicioId = 3,
            FechaHora = DateTime.UtcNow.AddDays(1)
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.BarberoId)
            .WithErrorMessage("El BarberoId es requerido");
    }

    [Fact]
    public void FechaHora_default_genera_error()
    {
        var input = new AgendamientoInput
        {
            ClienteId = 1,
            BarberoId = 2,
            ServicioId = 3,
            FechaHora = default
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.FechaHora)
            .WithErrorMessage("La FechaHora del agendamiento es requerida");
    }

    [Fact]
    public void Sin_servicio_ni_paquete_genera_error()
    {
        var input = new AgendamientoInput
        {
            ClienteId = 1,
            BarberoId = 2,
            FechaHora = DateTime.UtcNow.AddDays(1)
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("Debe especificar al menos un servicio o paquete");
    }
}
