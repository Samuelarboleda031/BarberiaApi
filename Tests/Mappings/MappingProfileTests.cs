using AutoMapper;
using FluentAssertions;
using BarberiaApi.Application.Mappings;

namespace BarberiaApi.Tests.Mappings;

public class MappingProfileTests
{
    [Fact]
    public void MappingProfile_se_puede_instanciar_sin_errores()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());

        var mapper = config.CreateMapper();

        mapper.Should().NotBeNull();
    }

    [Fact]
    public void No_hay_mapeos_duplicados_que_causen_excepcion()
    {
        Action act = () => new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());

        act.Should().NotThrow();
    }
}
