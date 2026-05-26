using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class CreditoBarbero
{
    public int Id { get; set; }

    public int BarberoId { get; set; }

    public decimal CupoMaximo { get; set; } = 200000;

    public decimal SaldoDeuda { get; set; } = 0;

    public string Estado { get; set; } = "Activo";

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual Barbero Barbero { get; set; } = null!;

    public virtual ICollection<AbonoCreditoBarbero> Abonos { get; set; } = new List<AbonoCreditoBarbero>();

    public virtual ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}
