namespace BarberiaApi.Application.DTOs;

// DTOs para Paquetes
public class DetallePaqueteInput
{
    public int ServicioId { get; set; }
    public int Cantidad { get; set; }
}

public class PaqueteInput
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int DuracionMinutos { get; set; }
}

public class PaqueteConDetallesInput
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int DuracionMinutos { get; set; }
    public List<DetallePaqueteInput> Detalles { get; set; } = new();
}
