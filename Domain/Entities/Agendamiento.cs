using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class Agendamiento
{
    public int Id { get; set; }

    /// <summary>
    /// Cliente registrado. Null cuando la cita es de un invitado (walk-in sin registro);
    /// en ese caso el nombre se guarda en <see cref="ClienteNombre"/>.
    /// </summary>
    public int? ClienteId { get; set; }

    /// <summary>
    /// Nombre libre del invitado cuando no hay <see cref="ClienteId"/>. Para clientes
    /// registrados queda null (el nombre se resuelve por la navegación Cliente.Usuario).
    /// </summary>
    public string? ClienteNombre { get; set; }

    public int BarberoId { get; set; }

    public int? ServicioId { get; set; }

    public int? PaqueteId { get; set; }

    public DateTime FechaHora { get; set; }

    public string? Estado { get; set; }
    
    public string? Duracion { get; set; }
    
    public decimal? Precio { get; set; }

    public string? Notas { get; set; }

    public virtual Barbero Barbero { get; set; } = null!;

    public virtual Cliente? Cliente { get; set; }

    public virtual Servicio? Servicio { get; set; }

    public virtual Paquete? Paquete { get; set; }
    
    public string? ServiciosRealizados { get; set; }
    public string? ServiciosPendientes { get; set; }
    public string? ProductosRealizados { get; set; }
    public string? ProductosPendientes { get; set; }
    public decimal? PrecioFinal { get; set; }
    public int? VentaAsociadaId { get; set; }

    public virtual Venta? VentaAsociada { get; set; }

    public virtual ICollection<AgendamientoProducto> AgendamientoProductos { get; set; } = new List<AgendamientoProducto>();

    public virtual ICollection<AgendamientoServicio> AgendamientoServicios { get; set; } = new List<AgendamientoServicio>();
}
