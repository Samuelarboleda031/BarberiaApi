namespace BarberiaApi.Application.DTOs;

// DTOs para Productos
public class ProductoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Marca { get; set; }
    public string? Tipo { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal PrecioCompra { get; set; }
    public int StockVentas { get; set; }
    public int StockInsumos { get; set; }
    public int StockTotal { get; set; }
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public bool? Estado { get; set; }
    public string? ImagenProduc { get; set; }
}

public class TransferirStockInput
{
    public int Cantidad { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
}
