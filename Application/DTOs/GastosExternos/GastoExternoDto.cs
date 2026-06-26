using System;

namespace BarberiaApi.Application.DTOs;

public class GastoExternoDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = null!;
    public decimal Monto { get; set; }
    public string Categoria { get; set; } = null!;
    public string Fecha { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = null!;
    public string? Notas { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
