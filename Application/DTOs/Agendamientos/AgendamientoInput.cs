namespace BarberiaApi.Application.DTOs;

// DTOs para Agendamientos
public class AgendamientoInput
{
    public int ClienteId { get; set; }
    public int BarberoId { get; set; }
    public int? ServicioId { get; set; }
    public List<int>? ServicioIds { get; set; }
    public int? PaqueteId { get; set; }

    /// <summary>
    /// Productos del agendamiento con cantidad. Reemplaza al obsoleto ProductoIds.
    /// </summary>
    public List<AgendamientoProductoInput>? Productos { get; set; }

    /// <summary>
    /// OBSOLETO: usar Productos[]. Se mantiene por compatibilidad: cada id se interpreta como Cantidad=1.
    /// </summary>
    [System.Obsolete("Usar Productos: [{ProductoId, Cantidad}]")]
    public List<int>? ProductoIds { get; set; }

    public DateTime FechaHora { get; set; }
    public string? Notas { get; set; }
    public string? Duracion { get; set; }
    public decimal? Precio { get; set; }
}

public class AgendamientoProductoInput
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; } = 1;
}

public class AgendamientoDTO
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int BarberoId { get; set; }
    public int? ServicioId { get; set; }
    public List<int> ServicioIds { get; set; } = new();
    public int? PaqueteId { get; set; }

    /// <summary>OBSOLETO: usar Productos. Se mantiene por compatibilidad.</summary>
    public List<int> ProductoIds { get; set; } = new();

    /// <summary>Productos del agendamiento con id, nombre, cantidad e imagen.</summary>
    public List<AgendamientoProductoDTO> Productos { get; set; } = new();

    /// <summary>Servicios del agendamiento con id, nombre, duración e imagen.</summary>
    public List<AgendamientoServicioDTO> Servicios { get; set; } = new();

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

public class AgendamientoProductoDTO
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string? Imagen { get; set; }
    public decimal? PrecioVenta { get; set; }
}

public class AgendamientoServicioDTO
{
    public int ServicioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? Duracion { get; set; }
    public string? Imagen { get; set; }
    public decimal? Precio { get; set; }
}
