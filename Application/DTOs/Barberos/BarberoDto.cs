namespace BarberiaApi.Application.DTOs;

// DTOs para Barberos
public class BarberoInput
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Barrio { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string Especialidad { get; set; } = "General";
    public string? FotoPerfil { get; set; }
    public bool Estado { get; set; } = true;
    public string Contrasena { get; set; } = string.Empty;
}

public class BarberoDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Barrio { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string Especialidad { get; set; } = string.Empty;
    public string? FotoPerfil { get; set; }
    public bool Estado { get; set; }
    public DateTime FechaContratacion { get; set; }
    public UsuarioDto? Usuario { get; set; }
}
