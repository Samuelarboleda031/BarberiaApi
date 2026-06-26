using System;

namespace BarberiaApi.Application.DTOs;

public class GastoExternoInput
{
    public string Descripcion { get; set; } = null!;
    public decimal Monto { get; set; }
    public string Categoria { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public string? Notas { get; set; }
}
