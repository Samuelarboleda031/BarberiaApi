using FluentAssertions;
using FluentValidation.TestHelper;
using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Validators;

namespace BarberiaApi.Tests.Validators;

public class CompraInputValidatorTests
{
    private readonly CompraInputValidator _validator = new();

    [Fact]
    public void Compra_valida_no_tiene_errores()
    {
        var input = new CompraInput
        {
            ProveedorId = 1,
            UsuarioId = 1,
            Detalles = new()
            {
                new() { ProductoId = 1, Cantidad = 10, PrecioUnitario = 5000 }
            }
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ProveedorId_cero_genera_error()
    {
        var input = new CompraInput
        {
            ProveedorId = 0,
            UsuarioId = 1,
            Detalles = new() { new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100 } }
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.ProveedorId)
            .WithErrorMessage("El ProveedorId es requerido");
    }

    [Fact]
    public void UsuarioId_cero_genera_error()
    {
        var input = new CompraInput
        {
            ProveedorId = 1,
            UsuarioId = 0,
            Detalles = new() { new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100 } }
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.UsuarioId)
            .WithErrorMessage("El UsuarioId es requerido");
    }

    [Fact]
    public void Detalles_null_genera_error()
    {
        var input = new CompraInput { ProveedorId = 1, UsuarioId = 1, Detalles = null! };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Detalles)
            .WithErrorMessage("Los detalles de la compra son requeridos");
    }

    [Fact]
    public void Detalles_vacio_genera_error()
    {
        var input = new CompraInput { ProveedorId = 1, UsuarioId = 1, Detalles = new() };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Detalles)
            .WithErrorMessage("La compra debe tener al menos un detalle");
    }

    [Fact]
    public void Detalle_cantidad_cero_genera_error()
    {
        var input = new CompraInput
        {
            ProveedorId = 1,
            UsuarioId = 1,
            Detalles = new() { new() { ProductoId = 1, Cantidad = 0, PrecioUnitario = 100 } }
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("La cantidad de cada detalle debe ser mayor a 0");
    }

    [Fact]
    public void Detalle_productoId_cero_genera_error()
    {
        var input = new CompraInput
        {
            ProveedorId = 1,
            UsuarioId = 1,
            Detalles = new() { new() { ProductoId = 0, Cantidad = 1, PrecioUnitario = 100 } }
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("El ProductoId es requerido en cada detalle");
    }

    [Fact]
    public void Detalle_precio_negativo_genera_error()
    {
        var input = new CompraInput
        {
            ProveedorId = 1,
            UsuarioId = 1,
            Detalles = new() { new() { ProductoId = 1, Cantidad = 5, PrecioUnitario = -200 } }
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("El precio unitario no puede ser negativo");
    }

    [Fact]
    public void Multiples_detalles_validos_no_generan_errores()
    {
        var input = new CompraInput
        {
            ProveedorId = 1,
            UsuarioId = 1,
            Detalles = new()
            {
                new() { ProductoId = 1, Cantidad = 10, PrecioUnitario = 5000 },
                new() { ProductoId = 2, Cantidad = 5, PrecioUnitario = 12000 },
                new() { ProductoId = 3, Cantidad = 20, PrecioUnitario = 3500 }
            }
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
