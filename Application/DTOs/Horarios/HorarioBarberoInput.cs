using System.Text.Json.Serialization;
namespace BarberiaApi.Application.DTOs;

// DTOs para Horarios de Barberos
public class HorarioBarberoCreateInput
{
    public int BarberoId { get; set; }
    public int DiaSemana { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
}

public class HorarioBarberoUpdateInput
{
    public int? BarberoId { get; set; }
    public int? DiaSemana { get; set; }
    public TimeSpan? HoraInicio { get; set; }
    public TimeSpan? HoraFin { get; set; }
    public bool? Estado { get; set; }
}

public class CambioEstadoHorarioInput
{
    public bool estado { get; set; }
    public int UsuarioSolicitanteId { get; set; }
    [JsonPropertyName("fechaHora")]
    public DateTime? FechaHora { get; set; }
    public DateTime? FechaReferencia { get; set; }
    public string? Motivo { get; set; }
    public int CantidadSugerencias { get; set; } = 3;
}
