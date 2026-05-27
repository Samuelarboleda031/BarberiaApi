using System;

namespace BarberiaApi.Domain.Entities;

public class HistorialEstadoCredito
{
    public int Id { get; set; }

    public int CreditoBarberoId { get; set; }

    public string EstadoAnterior { get; set; } = string.Empty;

    public string EstadoNuevo { get; set; } = string.Empty;

    public DateTime FechaCambio { get; set; }

    public int ResponsableId { get; set; }

    public string? Observacion { get; set; }

    public virtual CreditoBarbero CreditoBarbero { get; set; } = null!;

    public virtual Usuario Responsable { get; set; } = null!;
}
