using System;

namespace BarberiaApi.Domain.Entities;

public partial class AbonoCreditoBarbero
{
    public int Id { get; set; }

    public int CreditoBarberoId { get; set; }

    public int UsuarioId { get; set; }

    public int? VentaId { get; set; }

    public decimal Monto { get; set; }

    public string? MetodoPago { get; set; }

    public DateTime Fecha { get; set; }

    public string? Notas { get; set; }

    public string Estado { get; set; } = "Completado";

    public virtual CreditoBarbero CreditoBarbero { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Venta? Venta { get; set; }
}
