using System;

namespace BarberiaApi.Domain.Entities;

public partial class DescuentoDia
{
    public int Id { get; set; }

    /// <summary>Fecha del descuento (sin hora). Única por día.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Porcentaje de descuento aplicado a las citas de ese día (0-100).</summary>
    public decimal Porcentaje { get; set; }
}
