using System.ComponentModel.DataAnnotations;

namespace BarberiaApi.Application.DTOs;

public class SolicitudCambioHorarioCreateInput
{
    [Required]
    public int BarberoId { get; set; }

    [Required(ErrorMessage = "MotivoCategoria es requerido")]
    public string MotivoCategoria { get; set; } = string.Empty;

    public string? MotivoDetalle { get; set; }

    [Required]
    public DateTime FechaReferencia { get; set; }

    [MinLength(1, ErrorMessage = "Debe enviar al menos una sugerencia de horario")]
    public List<SugerenciaCambioHorarioInput> Sugerencias { get; set; } = new();
}

public class SugerenciaCambioHorarioInput
{
    [Required]
    public DateTime DiaSugerido { get; set; }

    [Required]
    public TimeSpan HoraInicio { get; set; }

    [Required]
    public TimeSpan HoraFin { get; set; }
}

public class SolicitudCambioHorarioRechazarInput
{
    public string? Observacion { get; set; }

    /// <summary>
    /// Sugerencias del administrador (opcional). Si se envían, el estado pasa a "Sugerida"
    /// para que el barbero responda; si está vacío, el estado pasa a "Rechazada".
    /// </summary>
    public List<SugerenciaCambioHorarioInput>? Sugerencias { get; set; }
}

public class SolicitudCambioHorarioRespuestaInput
{
    /// <summary>true = barbero acepta la contrapropuesta del admin; false = la rechaza.</summary>
    [Required]
    public bool Acepta { get; set; }
    public string? Observacion { get; set; }
}
