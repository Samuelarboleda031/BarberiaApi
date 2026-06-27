using System;

namespace BarberiaApi.Domain.Entities;

public partial class GastoExterno
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = null!;
    public decimal Monto { get; set; }
    public string Categoria { get; set; } = null!;  // 'Servicios', 'Suministros', etc.
    public DateOnly Fecha { get; set; }
    public int UsuarioId { get; set; }
    public string? UsuarioNombre { get; set; }
    public string? UsuarioApellido { get; set; }
    public string? UsuarioCorreo { get; set; }
    public string? UsuarioDocumento { get; set; }
    public string? UsuarioTipoDocumento { get; set; }
    public string? Notas { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool Estado { get; set; } = true;
    
    // Navegación
    public virtual Usuario Usuario { get; set; } = null!;
}
