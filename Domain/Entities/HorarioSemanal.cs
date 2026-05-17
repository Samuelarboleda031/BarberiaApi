using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class HorarioSemanal
{
    public int Id { get; set; }

    public int BarberoId { get; set; }

    public DateTime FechaInicioSemana { get; set; }

    public DateTime FechaFinSemana { get; set; }

    public string Estado { get; set; } = "Activo"; // Puede ser "Activo", "Pendiente", "Finalizado"

    public virtual Barbero Barbero { get; set; } = null!;

    public virtual ICollection<DetalleHorarioDia> Detalles { get; set; } = new List<DetalleHorarioDia>();
}
