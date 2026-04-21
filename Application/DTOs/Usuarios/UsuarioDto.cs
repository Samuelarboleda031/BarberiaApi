namespace BarberiaApi.Application.DTOs;

// DTOs para Usuarios
public class UsuarioInput
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Contrasena { get; set; }
    public int RolId { get; set; }
    public string? TipoDocumento { get; set; }
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Barrio { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? FotoPerfil { get; set; }
    public bool Estado { get; set; } = true;
}

public class UsuarioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int? RolId { get; set; }
    public string? RolNombre { get; set; }
    public string? TipoDocumento { get; set; }
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Barrio { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? FotoPerfil { get; set; }
    public bool Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public ClienteDto? Cliente { get; set; }
    public BarberoDto? Barbero { get; set; }
}

public class AnalisisUsuarioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int? RolId { get; set; }
    public string? RolNombre { get; set; }
    public bool EsCliente { get; set; }
    public bool EsBarbero { get; set; }
    public int VentasHechas { get; set; }
    public int ComprasHechas { get; set; }
    public int DevolucionesProcesadas { get; set; }
    public int EntregasRegistradas { get; set; }
    public int VentasComoCliente { get; set; }
    public int AgendamientosCliente { get; set; }
    public int DevolucionesCliente { get; set; }
    public int AgendamientosBarbero { get; set; }
    public int EntregasBarbero { get; set; }
    public List<string> ModulosAcceso { get; set; } = new();
}

public class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
