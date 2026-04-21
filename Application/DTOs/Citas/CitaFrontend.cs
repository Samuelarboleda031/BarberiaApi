namespace BarberiaApi.Application.DTOs;

// DTOs para Citas (compatibilidad frontend)
public class CitaFrontend
{
    public int id { get; set; }
    public string cliente { get; set; } = string.Empty;
    public string telefono { get; set; } = string.Empty;
    public string servicio { get; set; } = string.Empty;
    public string barbero { get; set; } = string.Empty;
    public string fecha { get; set; } = string.Empty;
    public string hora { get; set; } = string.Empty;
    public int duracion { get; set; }
    public decimal precio { get; set; }
    public string estado { get; set; } = string.Empty;
    public string notas { get; set; } = string.Empty;
}

public class CitaInputFrontend
{
    public string cliente { get; set; } = string.Empty;
    public string telefono { get; set; } = string.Empty;
    public string servicio { get; set; } = string.Empty;
    public string barbero { get; set; } = string.Empty;
    public string fecha { get; set; } = string.Empty;
    public string hora { get; set; } = string.Empty;
    public int duracion { get; set; }
    public decimal precio { get; set; }
    public string estado { get; set; } = string.Empty;
    public string notas { get; set; } = string.Empty;
}
