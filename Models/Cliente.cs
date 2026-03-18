using System;
using System.Collections.Generic;

namespace BarberiaApi.Models;

public partial class Cliente
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    public string? Barrio { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    public bool Estado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<Agendamiento> Agendamientos { get; set; } = new List<Agendamiento>();

    public virtual ICollection<Devolucion> Devoluciones { get; set; } = new List<Devolucion>();

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();

    public virtual Usuario Usuario { get; set; } = null!;
}
