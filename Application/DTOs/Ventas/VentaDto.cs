namespace BarberiaApi.Application.DTOs;

public class VentaDto
{
    public int Id { get; set; }
    public string? NumeroRecibo { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public decimal? IVA { get; set; }
    public decimal? Descuento { get; set; }
    public string? Estado { get; set; }
    public string? MetodoPago { get; set; }
    public string? TipoVenta { get; set; }
    public string? ClienteNombre { get; set; }
    public int? ClienteId { get; set; }
    public int? BarberoId { get; set; }
    public int UsuarioId { get; set; }
    public decimal? SaldoAFavorUsado { get; set; }
    
    // Proyecciones
    public string? ClienteNombreCompleto { get; set; }
    public string? BarberoNombreCompleto { get; set; }
    public string? UsuarioNombreCompleto { get; set; }
    
    public List<DetalleVentaDto> Detalles { get; set; } = new();
    
    public ClienteDto? Cliente { get; set; }
    public BarberoDto? Barbero { get; set; }
}

public class DetalleVentaDto
{
    public int Id { get; set; }
    public int? ProductoId { get; set; }
    public int? ServicioId { get; set; }
    public int? PaqueteId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    
    public string? ProductoNombre { get; set; }
    public string? ServicioNombre { get; set; }
    public string? PaqueteNombre { get; set; }
    public string? FotoUrl { get; set; }
}
