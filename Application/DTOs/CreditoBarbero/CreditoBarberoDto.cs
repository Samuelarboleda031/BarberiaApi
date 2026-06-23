namespace BarberiaApi.Application.DTOs;

public class CreditoBarberoDto
{
    public int Id { get; set; }
    public int BarberoId { get; set; }
    public string? BarberoNombre { get; set; }

    // Campos renombrados — mantenemos alias para compatibilidad frontend
    public decimal CupoMaximo { get; set; }       // = LimiteCredito
    public decimal SaldoDeuda { get; set; }        // = SaldoPendiente
    public decimal CupoDisponible { get; set; }

    public string Estado { get; set; } = string.Empty;
    public int PlazoDias { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaCierre { get; set; }
    public bool ExtensionUsada { get; set; }

    // Null siempre (columna eliminada — solo para compatibilidad JSON)
    public DateTime? FechaActualizacion { get; set; }

    // Calculados en GetAllAsync — eliminan N+1 calls en el frontend
    public DateTime? UltimoAbono { get; set; }
    public int VentasCicloCount { get; set; }
}

public class AbonoCreditoBarberoDto
{
    public int Id { get; set; }
    public int CreditoBarberoId { get; set; }
    public int UsuarioId { get; set; }
    public string? UsuarioNombre { get; set; }
    public int? VentaId { get; set; }
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public DateTime Fecha { get; set; }
    public string? Notas { get; set; }
    public string Estado { get; set; } = "Completado";
}

public class AbonoInput
{
    public int UsuarioId { get; set; }
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public string? Notas { get; set; }
    public int? VentaId { get; set; }
}

public class ExtenderPlazoInput
{
    public int UsuarioId { get; set; }
}

public class NuevoCicloInput
{
    public int UsuarioId { get; set; }
    public decimal? LimiteCredito { get; set; }
    public int PlazoDias { get; set; } = 7;
}

public class SubirLimiteInput
{
    public int UsuarioId { get; set; }
    public decimal Incremento { get; set; }
}
