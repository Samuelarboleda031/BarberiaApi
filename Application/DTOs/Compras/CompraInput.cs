namespace BarberiaApi.Application.DTOs;

// DTOs para Compras
public class CompraInput
{
    public int ProveedorId { get; set; }
    public int UsuarioId { get; set; }
    public string? NumeroFactura { get; set; }
    public DateTime? FechaFactura { get; set; }
    public string? MetodoPago { get; set; }
    public decimal? IVA { get; set; }
    public decimal? Descuento { get; set; }
    public List<DetalleCompraInput> Detalles { get; set; } = new();
}

public class DetalleCompraInput
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public int CantidadVentas { get; set; } = 0;
    public int CantidadInsumos { get; set; } = 0;
    public decimal PrecioUnitario { get; set; }
}
