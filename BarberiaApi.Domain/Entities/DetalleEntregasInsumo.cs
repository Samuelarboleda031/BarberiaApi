using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class DetalleEntregasInsumo
{
    public int Id { get; set; }

    public int EntregaId { get; set; }

    public int ProductoId { get; set; }

    public int Cantidad { get; set; }

    public decimal? PrecioHistorico { get; set; }

    public virtual EntregasInsumo Entrega { get; set; } = null!;

    public virtual Producto Producto { get; set; } = null!;
}
