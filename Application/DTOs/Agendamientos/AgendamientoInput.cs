namespace BarberiaApi.Application.DTOs;

// DTOs para Agendamientos
public class AgendamientoInput
{
    public int ClienteId { get; set; }
    public int BarberoId { get; set; }
    public int? ServicioId { get; set; }
    public List<int>? ServicioIds { get; set; }
    public int? PaqueteId { get; set; }
    public List<int>? ProductoIds { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Notas { get; set; }
    public string? Duracion { get; set; }
    public decimal? Precio { get; set; }
}

public class AgendamientoDTO
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int BarberoId { get; set; }
    public int? ServicioId { get; set; }
    public List<int> ServicioIds { get; set; } = new();
    public int? PaqueteId { get; set; }
    public List<int> ProductoIds { get; set; } = new();
    public string ClienteNombre { get; set; } = string.Empty;
    public string BarberoNombre { get; set; } = string.Empty;
    public string? ServicioNombre { get; set; }
    public List<string> ServiciosNombres { get; set; } = new();
    public List<string> ProductosNombres { get; set; } = new();
    public string? PaqueteNombre { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Estado { get; set; }
    public string? Duracion { get; set; }
    public decimal? Precio { get; set; }
    public string? Notas { get; set; }
}
