using System;

namespace BarberiaApi.Domain.Entities;

public partial class DetalleHorarioDia
{
    public int Id { get; set; }

    public int HorarioSemanalId { get; set; }

    public int DiaSemana { get; set; }

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFin { get; set; }

    public virtual HorarioSemanal HorarioSemanal { get; set; } = null!;
}
