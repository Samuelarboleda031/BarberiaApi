using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class AgendamientoProducto
{
    public int Id { get; set; }

    public int AgendamientoId { get; set; }

    public int ProductoId { get; set; }

    public int Cantidad { get; set; } = 1;

    public virtual Agendamiento Agendamiento { get; set; } = null!;

    public virtual Producto Producto { get; set; } = null!;
}
