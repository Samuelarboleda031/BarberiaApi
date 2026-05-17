using System;
using System.Collections.Generic;

namespace BarberiaApi.Application.DTOs;

public class HorarioSemanalDto
{
    public int Id { get; set; }
    public int BarberoId { get; set; }
    public string? BarberoNombre { get; set; }
    public DateTime FechaInicioSemana { get; set; }
    public DateTime FechaFinSemana { get; set; }
    public string Estado { get; set; } = string.Empty;
    public List<DetalleHorarioDiaDto> Detalles { get; set; } = new();
}

public class DetalleHorarioDiaDto
{
    public int Id { get; set; }
    public int HorarioSemanalId { get; set; }
    public int DiaSemana { get; set; }
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
}
