using FluentAssertions;
using FluentValidation.TestHelper;
using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Validators;

namespace BarberiaApi.Tests.Validators;

public class VentaInputValidatorTests
{
    private readonly VentaInputValidator _validator = new();

    [Fact]
    public void Venta_valida_no_tiene_errores()
    {
        var input = new VentaInput
        {
            UsuarioId = 1,
            Detalles = new List<DetalleVentaInput>
            {
                new() { ProductoId = 1, Cantidad = 2, PrecioUnitario = 15000 }
            }
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Detalles_null_genera_error()
    {
        var input = new VentaInput { UsuarioId = 1, Detalles = null! };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Detalles)
            .WithErrorMessage("Los detalles de la venta son requeridos");
    }

    [Fact]
    public void Detalles_vacio_genera_error()
    {
        var input = new VentaInput { UsuarioId = 1, Detalles = new() };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Detalles)
            .WithErrorMessage("La venta debe tener al menos un detalle");
    }

    [Fact]
    public void Cantidad_cero_genera_error()
    {
        var input = new VentaInput
        {
            UsuarioId = 1,
            Detalles = new() { new() { ProductoId = 1, Cantidad = 0, PrecioUnitario = 100 } }
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("La cantidad de cada detalle debe ser mayor a 0");
    }

    [Fact]
    public void Cantidad_negativa_genera_error()
    {
        var input = new VentaInput
        {
            UsuarioId = 1,
            Detalles = new() { new() { ProductoId = 1, Cantidad = -5, PrecioUnitario = 100 } }
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("La cantidad de cada detalle debe ser mayor a 0");
    }

    [Fact]
    public void PrecioUnitario_negativo_genera_error()
    {
        var input = new VentaInput
        {
            UsuarioId = 1,
            Detalles = new() { new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = -500 } }
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("El precio unitario no puede ser negativo");
    }

    [Fact]
    public void Detalle_sin_producto_servicio_ni_paquete_genera_error()
    {
        var input = new VentaInput
        {
            UsuarioId = 1,
            Detalles = new() { new() { Cantidad = 1, PrecioUnitario = 100 } }
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("Cada detalle debe tener un ProductoId, ServicioId o PaqueteId");
    }

    [Fact]
    public void Descuento_negativo_genera_error()
    {
        var input = new VentaInput
        {
            UsuarioId = 1,
            Descuento = -100,
            Detalles = new() { new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100 } }
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Descuento)
            .WithErrorMessage("El descuento no puede ser negativo");
    }

    [Fact]
    public void Detalle_con_servicio_es_valido()
    {
        var input = new VentaInput
        {
            UsuarioId = 1,
            Detalles = new() { new() { ServicioId = 5, Cantidad = 1, PrecioUnitario = 25000 } }
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Detalle_con_paquete_es_valido()
    {
        var input = new VentaInput
        {
            UsuarioId = 1,
            Detalles = new() { new() { PaqueteId = 3, Cantidad = 1, PrecioUnitario = 50000 } }
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
