using FluentAssertions;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Tests.DTOs;

public class ServiceResultTests
{
    [Fact]
    public void Ok_retorna_success_true_con_data()
    {
        var result = ServiceResult<string>.Ok("dato");

        result.Success.Should().BeTrue();
        result.Data.Should().Be("dato");
        result.StatusCode.Should().Be(200);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Fail_retorna_success_false_con_error()
    {
        var result = ServiceResult<string>.Fail("algo fallo");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("algo fallo");
        result.StatusCode.Should().Be(400);
        result.Data.Should().BeNull();
    }

    [Fact]
    public void Fail_con_codigo_personalizado()
    {
        var result = ServiceResult<string>.Fail("error interno", 500);

        result.StatusCode.Should().Be(500);
    }

    [Fact]
    public void NotFound_retorna_404()
    {
        var result = ServiceResult<string>.NotFound();

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error.Should().Be("Recurso no encontrado");
    }

    [Fact]
    public void NotFound_con_mensaje_personalizado()
    {
        var result = ServiceResult<string>.NotFound("Venta no encontrada");

        result.Error.Should().Be("Venta no encontrada");
        result.StatusCode.Should().Be(404);
    }
}
