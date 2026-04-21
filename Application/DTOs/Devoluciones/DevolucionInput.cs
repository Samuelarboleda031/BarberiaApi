namespace BarberiaApi.Application.DTOs;

// DTOs para Devoluciones
public class DevolucionInput
{
    public int? VentaId { get; set; }
    public int? ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public int? BarberoId { get; set; }
    public int? EntregaId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public string? MotivoCategoria { get; set; }
    public string? MotivoDetalle { get; set; }
    public string? Observaciones { get; set; }
    public decimal MontoDevuelto { get; set; }
    public decimal? SaldoAFavor { get; set; }
}

public class DevolucionUpdateInput
{
    public int Id { get; set; }
    public int? VentaId { get; set; }
    public int? ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public int? BarberoId { get; set; }
    public int? EntregaId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public string? MotivoCategoria { get; set; }
    public string? MotivoDetalle { get; set; }
    public string? Observaciones { get; set; }
    public decimal MontoDevuelto { get; set; }
    public decimal? SaldoAFavor { get; set; }
    public string? Estado { get; set; }
}

public class DevolucionBatchInput
{
    public int VentaId { get; set; }
    public int? ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public int? BarberoId { get; set; }
    public string MotivoCategoria { get; set; } = string.Empty;
    public string? MotivoDetalle { get; set; }
    public string? Observaciones { get; set; }
    public List<DevolucionItem> Items { get; set; } = new();
}

public class DevolucionItem
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal MontoDevuelto { get; set; }
}
