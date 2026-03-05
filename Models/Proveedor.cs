using System;
using System.Collections.Generic;

namespace BarberiaApi.Models;

public partial class Proveedor
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? NIT { get; set; }

    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    public bool? Estado { get; set; }

    // Campos específicos para tipo de proveedor
    public string TipoProveedor { get; set; } = "Natural"; // "Natural" o "Juridico"

    // Campos para Persona Jurídica
    public string? RazonSocial { get; set; }
    public string? RepresentanteLegal { get; set; }
    public string? NumeroIdentificacionRepLegal { get; set; }
    public string? CargoRepLegal { get; set; }
    public string? Ciudad { get; set; }
    public string? Departamento { get; set; }

    // Campos para Persona Natural
    public string? Contacto { get; set; }
    public string? NumeroIdentificacion { get; set; }
    public string? TipoIdentificacion { get; set; } // "CC", "CE", "TI", "Pasaporte"

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();
}
