using System.ComponentModel.DataAnnotations;
namespace BarberiaApi.Application.DTOs;

// DTOs para Proveedores
public class ProveedorNaturalInput
{
    // 1. Tipo de Proveedor (fijo "Natural" en este DTO)
    public string TipoProveedor { get; set; } = "Natural";

    // 2. Nombre proveedor
    [Required]
    public string Nombre { get; set; } = string.Empty;

    // 3. Tipo identificación prov.
    public string? TipoIdentificacionProveedor { get; set; } = "CC";

    // 4. Identificación
    [Required]
    public string Identificacion { get; set; } = string.Empty;

    // 5. Correo
    [Required]
    public string Correo { get; set; } = string.Empty;

    // 6. Teléfono
    [Required]
    public string Telefono { get; set; } = string.Empty;

    // 7. Dirección
    [Required]
    public string Direccion { get; set; } = string.Empty;

    // 8. Ciudad
    public string? Ciudad { get; set; }

    // 9. Departamento
    public string? Departamento { get; set; }

    // 10. Representante legal
    public string? RepresentanteLegal { get; set; }

    // 11. Tipo identificación rep.
    public string? TipoIdentificacionRepresentante { get; set; }

    // 12. Identificación rep.
    public string? IdentificacionRepresentante { get; set; }

    // 13. Correo representante
    public string? CorreoRepresentante { get; set; }

    // 14. Teléfono representante
    public string? TelefonoRepresentante { get; set; }
}

public class ProveedorJuridicoInput
{
    // 1. Tipo de Proveedor (fijo "Juridico" en este DTO)
    public string TipoProveedor { get; set; } = "Juridico";

    // 2. Nombre proveedor
    [Required]
    public string Nombre { get; set; } = string.Empty;

    // 3. Tipo identificación prov.
    public string? TipoIdentificacionProveedor { get; set; } = "NIT";

    // 4. Identificación
    [Required]
    public string Identificacion { get; set; } = string.Empty;

    // 5. Correo
    [Required]
    public string Correo { get; set; } = string.Empty;

    // 6. Teléfono
    [Required]
    public string Telefono { get; set; } = string.Empty;

    // 7. Dirección
    [Required]
    public string Direccion { get; set; } = string.Empty;

    // 8. Ciudad
    [Required(ErrorMessage = "Ciudad es obligatoria")]
    public string Ciudad { get; set; } = string.Empty;

    // 9. Departamento
    [Required(ErrorMessage = "Departamento es obligatorio")]
    public string Departamento { get; set; } = string.Empty;

    // 10. Representante legal
    [Required(ErrorMessage = "RepresentanteLegal es obligatorio para proveedores jurídicos")]
    public string RepresentanteLegal { get; set; } = string.Empty;

    // 11. Tipo identificación rep.
    public string? TipoIdentificacionRepresentante { get; set; } = "CC";

    // 12. Identificación rep.
    [Required(ErrorMessage = "IdentificacionRepresentante es obligatoria")]
    public string IdentificacionRepresentante { get; set; } = string.Empty;

    // 13. Correo representante
    [Required(ErrorMessage = "CorreoRepresentante es obligatorio")]
    public string CorreoRepresentante { get; set; } = string.Empty;

    // 14. Teléfono representante
    [Required(ErrorMessage = "TelefonoRepresentante es obligatorio")]
    public string TelefonoRepresentante { get; set; } = string.Empty;
}

public class ProveedorUpdateInput
{
    // 1
    public string? TipoProveedor { get; set; }
    // 2
    public string Nombre { get; set; } = string.Empty;
    // 3
    public string? TipoIdentificacionProveedor { get; set; }
    // 4
    public string? Identificacion { get; set; }
    // 5
    public string? Correo { get; set; }
    // 6
    public string? Telefono { get; set; }
    // 7
    public string? Direccion { get; set; }
    // 8
    public string? Ciudad { get; set; }
    // 9
    public string? Departamento { get; set; }
    // 10
    public string? RepresentanteLegal { get; set; }
    // 11
    public string? TipoIdentificacionRepresentante { get; set; }
    // 12
    public string? IdentificacionRepresentante { get; set; }
    // 13
    public string? CorreoRepresentante { get; set; }
    // 14
    public string? TelefonoRepresentante { get; set; }

    public bool? Estado { get; set; }
}

public class ProveedorCreateInput
{
    // 1. Tipo de Proveedor
    [Required]
    public string TipoProveedor { get; set; } = string.Empty;

    // 2. Nombre proveedor
    [Required]
    public string Nombre { get; set; } = string.Empty;

    // 3. Tipo identificación prov.
    public string? TipoIdentificacionProveedor { get; set; }

    // 4. Identificación
    [Required]
    public string Identificacion { get; set; } = string.Empty;

    // 5. Correo
    [Required]
    public string Correo { get; set; } = string.Empty;

    // 6. Teléfono
    [Required]
    public string Telefono { get; set; } = string.Empty;

    // 7. Dirección
    [Required]
    public string Direccion { get; set; } = string.Empty;

    // 8. Ciudad
    public string? Ciudad { get; set; }

    // 9. Departamento
    public string? Departamento { get; set; }

    // 10. Representante legal
    public string? RepresentanteLegal { get; set; }

    // 11. Tipo identificación rep.
    public string? TipoIdentificacionRepresentante { get; set; }

    // 12. Identificación rep.
    public string? IdentificacionRepresentante { get; set; }

    // 13. Correo representante
    public string? CorreoRepresentante { get; set; }

    // 14. Teléfono representante
    public string? TelefonoRepresentante { get; set; }
}
