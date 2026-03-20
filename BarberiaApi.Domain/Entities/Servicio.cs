using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarberiaApi.Domain.Entities;

public partial class Servicio
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal Precio { get; set; }

    [Column("DuracionMinutes")]
    public int? DuracionMinutos { get; set; }

    public bool? Estado { get; set; }
    
    public string? Imagen { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public virtual ICollection<Agendamiento> Agendamientos { get; set; } = new List<Agendamiento>();

    [System.Text.Json.Serialization.JsonIgnore]
    public virtual ICollection<DetalleVenta> DetalleVenta { get; set; } = new List<DetalleVenta>();

    [System.Text.Json.Serialization.JsonIgnore]
    public virtual ICollection<DetallePaquete> DetallePaquetes { get; set; } = new List<DetallePaquete>();
}
