using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class AgendamientoServicio
{
    public int Id { get; set; }

    public int AgendamientoId { get; set; }

    public int ServicioId { get; set; }

    public virtual Agendamiento Agendamiento { get; set; } = null!;

    public virtual Servicio Servicio { get; set; } = null!;
}
