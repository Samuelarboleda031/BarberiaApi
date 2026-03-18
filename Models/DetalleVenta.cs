using System;
using System.Collections.Generic;

namespace BarberiaApi.Models;

public partial class DetalleVenta
{
    public int Id { get; set; }

    public int VentaId { get; set; }

    public int? ProductoId { get; set; }

    public int? ServicioId { get; set; }

    public int? PaqueteId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }

    public virtual Paquete? Paquete { get; set; }

    public virtual Producto? Producto { get; set; }

    public virtual Servicio? Servicio { get; set; }

    public virtual Venta Venta { get; set; } = null!;
}
