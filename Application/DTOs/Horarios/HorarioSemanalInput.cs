using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BarberiaApi.Application.DTOs;

public class HorarioSemanalCreateInput
{
    public int BarberoId { get; set; }
    public DateTime FechaInicioSemana { get; set; }
    public DateTime FechaFinSemana { get; set; }
    public List<DetalleHorarioDiaCreateInput> Detalles { get; set; } = new();
}

public class DetalleHorarioDiaCreateInput
{
    public int DiaSemana { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
}

public class HorarioSemanalUpdateInput
{
    public DateTime? FechaInicioSemana { get; set; }
    public DateTime? FechaFinSemana { get; set; }
    public string? Estado { get; set; }
    public List<DetalleHorarioDiaCreateInput>? Detalles { get; set; }
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
