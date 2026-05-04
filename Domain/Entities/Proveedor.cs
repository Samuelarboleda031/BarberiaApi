using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class Proveedor
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? NIT { get; set; }

    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    public bool? Estado { get; set; }

    public string TipoProveedor { get; set; } = "Natural"; // "Natural" o "Juridico"

    public string? RepresentanteLegal { get; set; }
    public string? NumeroIdentificacion { get; set; }
    public string? TipoIdentificacion { get; set; } // "CC", "CE", "TI", "Pasaporte"
    public string? CorreoRepresentante { get; set; }
    public string? TelefonoRepresentante { get; set; }

    // Información adicional jurídica
    public string? Ciudad { get; set; }
    public string? Departamento { get; set; }

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();
}
