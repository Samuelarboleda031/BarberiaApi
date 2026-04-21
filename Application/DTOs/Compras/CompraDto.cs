namespace BarberiaApi.Application.DTOs;

public class CompraDto
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public int UsuarioId { get; set; }
    public string? NumeroFactura { get; set; }
    public DateOnly FechaFactura { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string? MetodoPago { get; set; }
    public decimal Subtotal { get; set; }
    public decimal? IVA { get; set; }
    public decimal? Descuento { get; set; }
    public decimal Total { get; set; }
    public string? Estado { get; set; }
    
    // Proyecciones
    public string? ProveedorNombre { get; set; }
    public string? ProveedorNIT { get; set; }
    public string? UsuarioNombreCompleto { get; set; }
    
    public List<DetalleCompraDto> DetalleCompras { get; set; } = new();
}

public class DetalleCompraDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string? ProductoNombre { get; set; }
    public int Cantidad { get; set; }
    public int CantidadVentas { get; set; }
    public int CantidadInsumos { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
