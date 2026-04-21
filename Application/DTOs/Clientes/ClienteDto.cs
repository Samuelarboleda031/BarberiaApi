namespace BarberiaApi.Application.DTOs;

// DTOs para Clientes
public class ClienteInput
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
    public string? FotoPerfil { get; set; }
    public bool Estado { get; set; } = true;
    public string Contrasena { get; set; } = string.Empty;
}

public class ClienteDto
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
    public string? FotoPerfil { get; set; }
    public bool Estado { get; set; }
    public DateTime FechaRegistro { get; set; }
    public UsuarioDto? Usuario { get; set; }
}
