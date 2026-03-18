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

    public string TipoProveedor { get; set; } = "Natural"; // "Natural" o "Juridico"

    public string? Contacto { get; set; }
    public string? NumeroIdentificacion { get; set; }
    public string? TipoIdentificacion { get; set; } // "CC", "CE", "TI", "Pasaporte"
    public string? CorreoContacto { get; set; }
    public string? TelefonoContacto { get; set; }

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();
}
