using System.ComponentModel.DataAnnotations;
namespace BarberiaApi.Application.DTOs;

// DTOs para Proveedores
public class ProveedorNaturalInput
{
    [Required]
    public string Nombre { get; set; } = string.Empty;
    [Required]
    public string NIT { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    [Required]
    public string Correo { get; set; } = string.Empty;
    [Required]
    public string Telefono { get; set; } = string.Empty;
    [Required]
    public string Direccion { get; set; } = string.Empty;
    public string? NumeroIdentificacion { get; set; }
    public string TipoIdentificacion { get; set; } = "CC";
    public string? CorreoContacto { get; set; }
    public string? TelefonoContacto { get; set; }
}

public class ProveedorJuridicoInput
{
    [Required]
    public string Nombre { get; set; } = string.Empty;
    [Required]
    public string NIT { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    [Required]
    public string Correo { get; set; } = string.Empty;
    [Required]
    public string Telefono { get; set; } = string.Empty;
    [Required]
    public string Direccion { get; set; } = string.Empty;
    public string? NumeroIdentificacion { get; set; }
    public string TipoIdentificacion { get; set; } = "NIT";
    public string? CorreoContacto { get; set; }
    public string? TelefonoContacto { get; set; }
}

public class ProveedorUpdateInput
{
    public string Nombre { get; set; } = string.Empty;
    public string? NIT { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool? Estado { get; set; }
    public string? Contacto { get; set; }
    public string? NumeroIdentificacion { get; set; }
    public string TipoIdentificacion { get; set; } = "CC";
    public string? CorreoContacto { get; set; }
    public string? TelefonoContacto { get; set; }
}

public class ProveedorCreateInput
{
    [Required]
    public string TipoProveedor { get; set; } = string.Empty;
    [Required]
    public string Nombre { get; set; } = string.Empty;
    [Required]
    public string NIT { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    [Required]
    public string Correo { get; set; } = string.Empty;
    [Required]
    public string Telefono { get; set; } = string.Empty;
    [Required]
    public string Direccion { get; set; } = string.Empty;
    public string? NumeroIdentificacion { get; set; }
    public string? TipoIdentificacion { get; set; }
    public string? CorreoContacto { get; set; }
    public string? TelefonoContacto { get; set; }
}
