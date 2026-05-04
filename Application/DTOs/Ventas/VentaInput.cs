namespace BarberiaApi.Application.DTOs;

// DTOs para Ventas
public class VentaInput
{
    public int UsuarioId { get; set; }
    public int? ClienteId { get; set; }
    public int? BarberoId { get; set; }
    public string? NumeroRecibo { get; set; }
    public string? TipoVenta { get; set; }
    public string? ClienteNombre { get; set; }
    public string? MetodoPago { get; set; }
    public decimal? Descuento { get; set; }
    public decimal? IVA { get; set; }
    public decimal? SaldoAFavorUsado { get; set; }
    public bool? UsarSaldoAFavor { get; set; }
    public List<DetalleVentaInput> Detalles { get; set; } = new();
}

public class DetalleVentaInput
{
    public int? ProductoId { get; set; }
    public int? ServicioId { get; set; }
    public int? PaqueteId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}
