using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class CreditoBarbero
{
    public int Id { get; set; }

    public int BarberoId { get; set; }

    public decimal LimiteCredito { get; set; } = 200000;

    public int PlazoDias { get; set; } = 7;

    public decimal SaldoPendiente { get; set; } = 0;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public DateTime? FechaCierre { get; set; }

    public string Estado { get; set; } = "Activo";

    public bool ExtensionUsada { get; set; } = false;

    public int CreadoPor { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Barbero Barbero { get; set; } = null!;

    public virtual Usuario CreadoPorUsuario { get; set; } = null!;

    public virtual ICollection<AbonoCreditoBarbero> Abonos { get; set; } = new List<AbonoCreditoBarbero>();

    public virtual ICollection<Venta> Ventas { get; set; } = new List<Venta>();

    public virtual ICollection<HistorialEstadoCredito> Historial { get; set; } = new List<HistorialEstadoCredito>();
}
