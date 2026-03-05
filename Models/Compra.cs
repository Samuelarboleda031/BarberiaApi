using System;
using System.Collections.Generic;

namespace BarberiaApi.Models;

public partial class Compra
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public int UsuarioId { get; set; }

    public string? NumeroFactura { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public DateOnly? FechaFactura { get; set; }

    public decimal Subtotal { get; set; }

    public decimal? IVA { get; set; }

    public decimal? Descuento { get; set; }

    public decimal Total { get; set; }

    public string? MetodoPago { get; set; }

    public string? Estado { get; set; }

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();

    public virtual Proveedor Proveedor { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
