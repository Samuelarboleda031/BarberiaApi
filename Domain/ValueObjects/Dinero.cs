namespace BarberiaApi.Domain.ValueObjects;

/// <summary>
/// Objeto de valor que representa una cantidad de dinero con moneda.
/// Inmutable por diseño (record). Usarlo para cálculos de totales,
/// descuentos y comisiones evita errores de tipo decimal sin moneda.
/// </summary>
public record Dinero(decimal Monto, string Moneda = "COP")
{
    public static Dinero Zero => new(0);
    public bool EsPositivo => Monto > 0;
    public Dinero Sumar(Dinero otro) => this with { Monto = Monto + otro.Monto };
    public Dinero Restar(Dinero otro) => this with { Monto = Monto - otro.Monto };
    public Dinero Porcentaje(decimal pct) => this with { Monto = Monto * pct / 100m };
    public override string ToString() => $"{Moneda} {Monto:N0}";
}
