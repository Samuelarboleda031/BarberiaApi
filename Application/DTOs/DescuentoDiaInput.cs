namespace BarberiaApi.Application.DTOs;

/// <summary>Entrada para crear/actualizar el descuento de un día específico.</summary>
public class DescuentoDiaInput
{
    /// <summary>Fecha en formato "yyyy-MM-dd".</summary>
    public string Fecha { get; set; } = string.Empty;

    /// <summary>Porcentaje de descuento (0-100). Si es 0 se elimina el descuento del día.</summary>
    public decimal Porcentaje { get; set; }
}
