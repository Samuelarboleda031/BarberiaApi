using System;
using System.Globalization;

namespace BarberiaApi.Application.DTOs;

public class GastoExternoInput
{
    public string Descripcion { get; set; } = null!;
    public decimal Monto { get; set; }
    public string Categoria { get; set; } = null!;
    public string Fecha { get; set; } = string.Empty;
    public string? Notas { get; set; }
}
