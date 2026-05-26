namespace BarberiaApi.Application.DTOs;

public class CreditoBarberoDto
{
    public int Id { get; set; }
    public int BarberoId { get; set; }
    public string? BarberoNombre { get; set; }
    public decimal CupoMaximo { get; set; }
    public decimal SaldoDeuda { get; set; }
    public decimal CupoDisponible { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

public class AbonoCreditoBarberoDto
{
    public int Id { get; set; }
    public int CreditoBarberoId { get; set; }
    public int UsuarioId { get; set; }
    public string? UsuarioNombre { get; set; }
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public DateTime Fecha { get; set; }
    public string? Notas { get; set; }
    public string Estado { get; set; } = "Activo";
}

public class AbonoInput
{
    public int UsuarioId { get; set; }
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public string? Notas { get; set; }
}

public class AnularAbonoInput
{
    public int UsuarioId { get; set; }
}
