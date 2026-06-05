using FluentAssertions;
using BarberiaApi.Application.Helpers;

namespace BarberiaApi.Tests.Helpers;

public class ValidationHelperTests
{
    [Fact]
    public void Url_null_es_valida()
    {
        var result = ValidationHelper.ValidarUrlImagen(null, out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Url_vacia_es_valida()
    {
        var result = ValidationHelper.ValidarUrlImagen("", out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Url_https_valida()
    {
        var result = ValidationHelper.ValidarUrlImagen("https://example.com/image.jpg", out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Url_http_valida()
    {
        var result = ValidationHelper.ValidarUrlImagen("http://example.com/photo.png", out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Url_relativa_valida()
    {
        var result = ValidationHelper.ValidarUrlImagen("/assets/images/foto.jpg", out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Url_relativa_sin_slash_genera_error()
    {
        var result = ValidationHelper.ValidarUrlImagen("images/foto.jpg", out var error);

        result.Should().BeFalse();
        error.Should().Contain("debe comenzar con /");
    }
}
