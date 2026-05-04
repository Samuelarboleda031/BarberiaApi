using System.ComponentModel.DataAnnotations;
namespace BarberiaApi.Application.DTOs;

// DTOs para Proveedores
public class ProveedorNaturalInput
{
    [Required]
    public string Nombre { get; set; } = string.Empty;
    [Required]
    public string NIT { get; set; } = string.Empty;
    public string? RepresentanteLegal { get; set; }
    [Required]
    public string Correo { get; set; } = string.Empty;
    [Required]
    public string Telefono { get; set; } = string.Empty;
    [Required]
    public string Direccion { get; set; } = string.Empty;
    public string? NumeroIdentificacion { get; set; }
    public string TipoIdentificacion { get; set; } = "CC";
    public string? CorreoRepresentante { get; set; }
    public string? TelefonoRepresentante { get; set; }
}

public class ProveedorJuridicoInput
{
    [Required]
    public string Nombre { get; set; } = string.Empty;
    [Required]
    public string NIT { get; set; } = string.Empty;
    [Required(ErrorMessage = "RepresentanteLegal es obligatorio para proveedores jurídicos")]
    public string RepresentanteLegal { get; set; } = string.Empty;
    [Required]
    public string Correo { get; set; } = string.Empty;
    [Required]
    public string Telefono { get; set; } = string.Empty;
    [Required]
    public string Direccion { get; set; } = string.Empty;
    [Required(ErrorMessage = "NumeroIdentificacion del representante es obligatorio")]
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string TipoIdentificacion { get; set; } = "NIT";
    [Required(ErrorMessage = "CorreoRepresentante es obligatorio")]
    public string CorreoRepresentante { get; set; } = string.Empty;
    [Required(ErrorMessage = "TelefonoRepresentante es obligatorio")]
    public string TelefonoRepresentante { get; set; } = string.Empty;

    // Información adicional jurídica obligatoria
    [Required(ErrorMessage = "Ciudad es obligatoria")]
    public string Ciudad { get; set; } = string.Empty;
    [Required(ErrorMessage = "Departamento es obligatorio")]
    public string Departamento { get; set; } = string.Empty;
}

public class ProveedorUpdateInput
{
    public string Nombre { get; set; } = string.Empty;
    public string? NIT { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool? Estado { get; set; }
    public string? RepresentanteLegal { get; set; }
    public string? NumeroIdentificacion { get; set; }
    public string TipoIdentificacion { get; set; } = "CC";
    public string? CorreoRepresentante { get; set; }
    public string? TelefonoRepresentante { get; set; }
    public string? Ciudad { get; set; }
    public string? Departamento { get; set; }
}

public class ProveedorCreateInput
{
    [Required]
    public string TipoProveedor { get; set; } = string.Empty;
    [Required]
    public string Nombre { get; set; } = string.Empty;
    [Required]
    public string NIT { get; set; } = string.Empty;
    public string? RepresentanteLegal { get; set; }
    [Required]
    public string Correo { get; set; } = string.Empty;
    [Required]
    public string Telefono { get; set; } = string.Empty;
    [Required]
    public string Direccion { get; set; } = string.Empty;
    public string? NumeroIdentificacion { get; set; }
    public string? TipoIdentificacion { get; set; }
    public string? CorreoRepresentante { get; set; }
    public string? TelefonoRepresentante { get; set; }
    public string? Ciudad { get; set; }
    public string? Departamento { get; set; }
}
