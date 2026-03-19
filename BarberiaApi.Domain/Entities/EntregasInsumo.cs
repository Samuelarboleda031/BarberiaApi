using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class EntregasInsumo
{
    public int Id { get; set; }

    public int BarberoId { get; set; }

    public int UsuarioId { get; set; }

    public DateTime? Fecha { get; set; }

    public int CantidadTotal { get; set; }

    public decimal ValorTotal { get; set; }

    public string? Estado { get; set; }

    public virtual Barbero Barbero { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual ICollection<DetalleEntregasInsumo> DetalleEntregasInsumos { get; set; } = new List<DetalleEntregasInsumo>();

    public virtual ICollection<Devolucion> Devoluciones { get; set; } = new List<Devolucion>();
}
