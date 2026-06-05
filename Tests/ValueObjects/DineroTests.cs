using FluentAssertions;
using BarberiaApi.Domain.ValueObjects;

namespace BarberiaApi.Tests.ValueObjects;

public class DineroTests
{
    [Fact]
    public void Zero_retorna_monto_cero()
    {
        var dinero = Dinero.Zero;

        dinero.Monto.Should().Be(0);
        dinero.Moneda.Should().Be("COP");
    }

    [Fact]
    public void EsPositivo_true_cuando_monto_mayor_a_cero()
    {
        var dinero = new Dinero(15000);

        dinero.EsPositivo.Should().BeTrue();
    }

    [Fact]
    public void EsPositivo_false_cuando_monto_es_cero()
    {
        var dinero = Dinero.Zero;

        dinero.EsPositivo.Should().BeFalse();
    }

    [Fact]
    public void EsPositivo_false_cuando_monto_negativo()
    {
        var dinero = new Dinero(-5000);

        dinero.EsPositivo.Should().BeFalse();
    }

    [Fact]
    public void Sumar_retorna_suma_correcta()
    {
        var a = new Dinero(10000);
        var b = new Dinero(5000);

        var resultado = a.Sumar(b);

        resultado.Monto.Should().Be(15000);
        resultado.Moneda.Should().Be("COP");
    }

    [Fact]
    public void Restar_retorna_resta_correcta()
    {
        var a = new Dinero(20000);
        var b = new Dinero(7500);

        var resultado = a.Restar(b);

        resultado.Monto.Should().Be(12500);
    }

    [Fact]
    public void Porcentaje_calcula_correctamente()
    {
        var dinero = new Dinero(100000);

        var resultado = dinero.Porcentaje(19);

        resultado.Monto.Should().Be(19000);
    }

    [Fact]
    public void ToString_formato_correcto()
    {
        var dinero = new Dinero(50000);

        dinero.ToString().Should().Contain("COP");
        dinero.ToString().Should().Contain("50");
    }

    [Fact]
    public void Moneda_por_defecto_es_COP()
    {
        var dinero = new Dinero(1000);

        dinero.Moneda.Should().Be("COP");
    }

    [Fact]
    public void Moneda_personalizada_se_respeta()
    {
        var dinero = new Dinero(100, "USD");

        dinero.Moneda.Should().Be("USD");
    }

    [Fact]
    public void Igualdad_por_valor()
    {
        var a = new Dinero(10000, "COP");
        var b = new Dinero(10000, "COP");

        a.Should().Be(b);
    }

    [Fact]
    public void Desigualdad_por_monto()
    {
        var a = new Dinero(10000);
        var b = new Dinero(20000);

        a.Should().NotBe(b);
    }
}
