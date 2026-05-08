using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class Proveedor
{
    public int Id { get; set; }

    // 1. Tipo de Proveedor ("Natural" o "Juridico")
    public string TipoProveedor { get; set; } = "Natural";

    // 2. Nombre proveedor
    public string Nombre { get; set; } = null!;

    // 3. Tipo identificación prov. (CC, CE, NIT, Pasaporte, etc.)
    public string? TipoIdentificacionProveedor { get; set; }

    // 4. Identificación
    public string? Identificacion { get; set; }

    // 5. Correo
    public string? Correo { get; set; }

    // 6. Teléfono
    public string? Telefono { get; set; }

    // 7. Dirección
    public string? Direccion { get; set; }

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

    public bool? Estado { get; set; }

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();
}
