using System.Collections.Generic;

namespace BarberiaApi.Application.DTOs;

public class ResumenRangoDto
{
    public string Desde { get; set; } = string.Empty;
    public string Hasta { get; set; } = string.Empty;
    public decimal IngresosVentas { get; set; }
    public decimal IngresosAgendamientos { get; set; }
    public decimal IngresosTotal { get; set; }
    public decimal GastosExternos { get; set; }
    public decimal GananciaNeta { get; set; }
    public int CantidadGastos { get; set; }
    public List<GastoExternoDto> Gastos { get; set; } = new();
}
