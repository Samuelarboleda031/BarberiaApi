namespace BarberiaApi.Application.DTOs;

// DTOs para Entregas de Insumos
public class EntregaInput
{
    public int BarberoId { get; set; }
    public int UsuarioId { get; set; }
    public List<DetalleEntregaInput> Detalles { get; set; } = new();
    public string? MotivoCategoria { get; set; }
    public string? MotivoDetalle { get; set; }
}

public class DetalleEntregaInput
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal? PrecioHistorico { get; set; }
}

public class DetalleEntregaUpdateInput
{
    public int Cantidad { get; set; }
    public decimal? PrecioHistorico { get; set; }
}

public class DetalleEntregaIndividualInput
{
    public int EntregaId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal? PrecioHistorico { get; set; }
}
