using System;
using System.Collections.Generic;

namespace BarberiaApi.Models;

public partial class Agendamiento
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public int BarberoId { get; set; }

    public int? ServicioId { get; set; }

    public int? PaqueteId { get; set; }

    public DateTime FechaHora { get; set; }

    public string? Estado { get; set; }
    
    public string? Duracion { get; set; }
    
    public decimal? Precio { get; set; }

    public string? Notas { get; set; }

    public virtual Barbero Barbero { get; set; } = null!;

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual Servicio? Servicio { get; set; }

    public virtual Paquete? Paquete { get; set; }

}
