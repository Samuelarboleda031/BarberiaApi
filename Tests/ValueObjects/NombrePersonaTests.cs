using FluentAssertions;
using BarberiaApi.Domain.ValueObjects;

namespace BarberiaApi.Tests.ValueObjects;

public class NombrePersonaTests
{
    [Fact]
    public void NombreCompleto_combina_nombre_y_apellido()
    {
        var nombre = new NombrePersona("Carlos", "Rodriguez");

        nombre.NombreCompleto.Should().Be("Carlos Rodriguez");
    }

    [Fact]
    public void ToString_retorna_nombre_completo()
    {
        var nombre = new NombrePersona("Maria", "Lopez");

        nombre.ToString().Should().Be("Maria Lopez");
    }

    [Fact]
    public void NombreCompleto_trim_espacios()
    {
        var nombre = new NombrePersona("Juan", "");

        nombre.NombreCompleto.Should().Be("Juan");
    }

    [Fact]
    public void Igualdad_por_valor()
    {
        var a = new NombrePersona("Ana", "Garcia");
        var b = new NombrePersona("Ana", "Garcia");

        a.Should().Be(b);
    }

    [Fact]
    public void Desigualdad_por_apellido()
    {
        var a = new NombrePersona("Pedro", "Martinez");
        var b = new NombrePersona("Pedro", "Gomez");

        a.Should().NotBe(b);
    }
}
