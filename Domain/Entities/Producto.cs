using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class Producto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }
    
    public string? Marca { get; set; }
    public decimal PrecioVenta { get; set; }

    public decimal PrecioCompra { get; set; } = 0;

    public int Stock { get; set; } = 0;

    public int? CategoriaId { get; set; }

    public bool? Estado { get; set; }

    public string? ImagenProduc { get; set; }
    
    public virtual Categoria? Categoria { get; set; }

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();

    public virtual ICollection<DetalleVenta> DetalleVenta { get; set; } = new List<DetalleVenta>();

    public virtual ICollection<Devolucion> Devoluciones { get; set; } = new List<Devolucion>();

    public virtual ICollection<DetallePaquete> DetallePaquetes { get; set; } = new List<DetallePaquete>();

    public virtual ICollection<AgendamientoProducto> AgendamientoProductos { get; set; } = new List<AgendamientoProducto>();
}
