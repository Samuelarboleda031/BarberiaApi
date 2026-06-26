using System;
using System.Collections.Generic;

namespace BarberiaApi.Application.DTOs;

public class ResumenDiaDto
{
    public string Fecha { get; set; } = string.Empty;
    public decimal IngresosVentas { get; set; }      // Ventas completadas
    public decimal IngresosAgendamientos { get; set; } // Agendamientos completados
    public decimal IngresosTotal { get; set; }        // Total de ingresos
    public decimal GastosExternos { get; set; }        // Total gastos externos
    public decimal GananciaNeta { get; set; }          // IngresosTotal - GastosExternos
    public int CantidadGastos { get; set; }
    public List<GastoExternoDto> Gastos { get; set; } = new();
}
