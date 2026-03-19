using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class Venta
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int? ClienteId { get; set; }

    public int? BarberoId { get; set; }

    public DateTime? Fecha { get; set; }

    public decimal Subtotal { get; set; }

    public decimal? IVA { get; set; }

    public decimal? Descuento { get; set; }

    public decimal Total { get; set; }

    public string? MetodoPago { get; set; }

    public string? Estado { get; set; }
    
    public decimal? SaldoAFavorUsado { get; set; }

    public virtual Cliente? Cliente { get; set; }

    public virtual Barbero? Barbero { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual ICollection<DetalleVenta> DetalleVenta { get; set; } = new List<DetalleVenta>();

    public virtual ICollection<Devolucion> Devoluciones { get; set; } = new List<Devolucion>();
}
