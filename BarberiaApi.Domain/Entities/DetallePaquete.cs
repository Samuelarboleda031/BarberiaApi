using System;

namespace BarberiaApi.Domain.Entities;

public partial class DetallePaquete
{
    public int Id { get; set; }

    public int PaqueteId { get; set; }

    public int ServicioId { get; set; }

    public int Cantidad { get; set; }

    public int? ProductoId { get; set; }

    public virtual Paquete Paquete { get; set; } = null!;

    public virtual Servicio? Servicio { get; set; }

    public virtual Producto? Producto { get; set; }
}
