using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class SolicitudCambioHorario
{
    public int Id { get; set; }

    public int BarberoId { get; set; }

    public string MotivoCategoria { get; set; } = null!;

    public string? MotivoDetalle { get; set; }

    public DateTime FechaReferencia { get; set; }

    public string Estado { get; set; } = "Pendiente"; // Pendiente, Aprobada, Rechazada, Sugerida

    public string? ObservacionAdmin { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public int? UsuarioResolucionId { get; set; }

    public virtual Barbero Barbero { get; set; } = null!;

    public virtual Usuario? UsuarioResolucion { get; set; }

    public virtual ICollection<SugerenciaCambioHorario> Sugerencias { get; set; } = new List<SugerenciaCambioHorario>();
}

public partial class SugerenciaCambioHorario
{
    public int Id { get; set; }

    public int SolicitudId { get; set; }

    public DateTime DiaSugerido { get; set; }

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFin { get; set; }

    public string Origen { get; set; } = "Barbero"; // Barbero o Admin

    public virtual SolicitudCambioHorario Solicitud { get; set; } = null!;
}
