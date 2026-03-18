using System;
using System.Collections.Generic;

namespace BarberiaApi.Models;

public partial class HorariosBarbero
{
    public int Id { get; set; }

    public int BarberoId { get; set; }

    public int DiaSemana { get; set; }

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFin { get; set; }

    public bool? Estado { get; set; }

    public virtual Barbero Barbero { get; set; } = null!;
}
