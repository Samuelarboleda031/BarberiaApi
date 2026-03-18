using System;
using System.Collections.Generic;

namespace BarberiaApi.Models;

public partial class Barbero
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    
    public string? Direccion { get; set; }

    public string? Barrio { get; set; }

    public string? Telefono { get; set; }

    public string Especialidad { get; set; } = "General";

    public bool Estado { get; set; }
    
    public DateTime? FechaNacimiento { get; set; }

    public DateTime FechaContratacion { get; set; }

    public virtual ICollection<Agendamiento> Agendamientos { get; set; } = new List<Agendamiento>();

    public virtual ICollection<EntregasInsumo> EntregasInsumos { get; set; } = new List<EntregasInsumo>();

    public virtual ICollection<HorariosBarbero> Horarios { get; set; } = new List<HorariosBarbero>();

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();

    public virtual Usuario Usuario { get; set; } = null!;
}
