using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class Devolucion
{
    public int Id { get; set; }

    public int? VentaId { get; set; }

    public int? ClienteId { get; set; }

    public int UsuarioId { get; set; }

    public int? ProductoId { get; set; }

    public int? BarberoId { get; set; }

    public int? EntregaId { get; set; }

    public int Cantidad { get; set; }

    public string? MotivoCategoria { get; set; }

    public string? MotivoDetalle { get; set; }

    public string? Observaciones { get; set; }

    public DateTime? Fecha { get; set; }

    public decimal MontoDevuelto { get; set; }

    public string? Estado { get; set; }

    public decimal? SaldoAFavor { get; set; }

    public virtual Producto? Producto { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Venta? Venta { get; set; }

    public virtual Cliente? Cliente { get; set; }

    public virtual Barbero? Barbero { get; set; }

    public virtual EntregasInsumo? Entrega { get; set; }
}
