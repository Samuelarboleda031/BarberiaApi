using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class Usuario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string? Telefono { get; set; }

    public string Contrasena { get; set; } = null!;

    public int? RolId { get; set; }

    public string? TipoDocumento { get; set; }

    public string? Documento { get; set; }

    public string? FotoPerfil { get; set; }

    public bool Estado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public virtual ICollection<Devolucion> Devoluciones { get; set; } = new List<Devolucion>();

    public virtual ICollection<Venta> Ventas { get; set; } = new List<Venta>();

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();

    public virtual ICollection<AbonoCreditoBarbero> AbonosCreditoBarbero { get; set; } = new List<AbonoCreditoBarbero>();

    public virtual ICollection<GastoExterno> GastosExternos { get; set; } = new List<GastoExterno>();

    public virtual Cliente? Cliente { get; set; }

    public virtual Barbero? Barbero { get; set; }

    public virtual Role? Rol { get; set; }
}
