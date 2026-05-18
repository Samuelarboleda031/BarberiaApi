using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BarberiaApi.Application.Services;

public class HorarioService : IHorarioService
{
    private readonly BarberiaContext _context;
    private readonly INotificacionCitasService _notificacionCitasService;

    public HorarioService(BarberiaContext context, INotificacionCitasService notificacionCitasService)
    {
        _context = context;
        _notificacionCitasService = notificacionCitasService;
    }

    private static int DiaSemanaDominicalANumerico(DayOfWeek dayOfWeek)
    {
        var dia = (int)dayOfWeek;
        return dia == 0 ? 7 : dia;
    }

    private static bool PuedeGestionarDesactivacion(Usuario usuarioSolicitante, int barberoId)
    {
        var nombreRol = (usuarioSolicitante.Rol?.Nombre ?? string.Empty).Trim().ToLowerInvariant();
        var rolId = usuarioSolicitante.RolId ?? 0;

        var esSuperAdministrador = rolId == 18 || (nombreRol.Contains("super") && nombreRol.Contains("admin"));
        var esAdministrador = rolId == 1 || (nombreRol.Contains("admin") && !nombreRol.Contains("barbero") && !nombreRol.Contains("super"));
        var esBarbero = rolId == 2 || nombreRol.Contains("barbero");

        if (esSuperAdministrador || esAdministrador) return true;
        if (esBarbero)
        {
            return usuarioSolicitante.Barbero != null && usuarioSolicitante.Barbero.Id == barberoId;
        }

        return false;
    }

    private static int ObtenerDuracionMinutos(Agendamiento agendamiento)
    {
        if (!string.IsNullOrWhiteSpace(agendamiento.Duracion))
        {
            var match = Regex.Match(agendamiento.Duracion, @"\d+");
            if (match.Success && int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutos) && minutos > 0)
            {
                return minutos;
            }
        }

        if (agendamiento.Servicio?.DuracionMinutos.HasValue == true && agendamiento.Servicio.DuracionMinutos.Value > 0)
        {
            return agendamiento.Servicio.DuracionMinutos.Value;
        }
        if (agendamiento.Paquete?.DuracionMinutos > 0) return agendamiento.Paquete.DuracionMinutos;
        return 30;
    }

    private static string AgregarNotaSistema(string? notasPrevias, string motivo, DateTime fechaDesactivada)
    {
        var notaSistema = $"[Cancelación automática] Día desactivado ({fechaDesactivada:yyyy-MM-dd}). Motivo: {motivo}";
        if (string.IsNullOrWhiteSpace(notasPrevias))
        {
            return notaSistema;
        }

        return $"{notasPrevias.Trim()} | {notaSistema}";
    }

    /// <summary>
    /// Obtiene el HorarioSemanal activo para un barbero en una fecha dada.
    /// </summary>
    private async Task<HorarioSemanal?> ObtenerHorarioSemanalActivoAsync(int barberoId, DateTime fecha)
    {
        var fechaDate = fecha.Date;
        return await _context.HorariosSemanales
            .Include(h => h.Detalles)
            .FirstOrDefaultAsync(h => h.BarberoId == barberoId
                                      && h.Estado == "Activo"
                                      && h.FechaInicioSemana <= fechaDate
                                      && h.FechaFinSemana >= fechaDate);
    }

    /// <summary>
    /// Busca el detalle de un día específico dentro de los horarios activos de un barbero para una fecha.
    /// </summary>
    public async Task<DetalleHorarioDia?> ObtenerDetalleDiaActivoAsync(int barberoId, DateTime fecha)
    {
        var diaSemana = DiaSemanaDominicalANumerico(fecha.DayOfWeek);
        var horarioSemanal = await ObtenerHorarioSemanalActivoAsync(barberoId, fecha);
        return horarioSemanal?.Detalles.FirstOrDefault(d => d.DiaSemana == diaSemana);
    }

    private List<DateTime> ObtenerSugerenciasReprogramacion(
        int agendamientoIdActual,
        DateTime fechaOriginal,
        int duracionMinutos,
        int cantidadSugerencias,
        HorarioSemanal? horarioSemanal,
        Dictionary<DateTime, List<Agendamiento>> agendaPorDia)
    {
        var sugerencias = new List<DateTime>();
        if (horarioSemanal == null || horarioSemanal.Detalles.Count == 0) return sugerencias;

        var fechaInicioBusqueda = fechaOriginal.Date.AddDays(1);
        var fechaLimite = fechaInicioBusqueda.AddDays(30);

        for (var fecha = fechaInicioBusqueda; fecha <= fechaLimite && sugerencias.Count < cantidadSugerencias; fecha = fecha.AddDays(1))
        {
            var diaSemana = DiaSemanaDominicalANumerico(fecha.DayOfWeek);
            var horarioDia = horarioSemanal.Detalles.FirstOrDefault(h => h.DiaSemana == diaSemana);
            if (horarioDia == null) continue;

            var agendamientosDia = agendaPorDia.TryGetValue(fecha.Date, out var listaDia)
                ? listaDia.Where(a => a.Id != agendamientoIdActual).ToList()
                : new List<Agendamiento>();

            var inicioSlot = fecha.Add(horarioDia.HoraInicio);
            var limiteSlot = fecha.Add(horarioDia.HoraFin).AddMinutes(-duracionMinutos);

            while (inicioSlot <= limiteSlot && sugerencias.Count < cantidadSugerencias)
            {
                if (inicioSlot > DateTime.Now)
                {
                    var finSlot = inicioSlot.AddMinutes(duracionMinutos);
                    var hayTraslape = agendamientosDia.Any(a =>
                    {
                        var inicioExistente = a.FechaHora;
                        var finExistente = inicioExistente.AddMinutes(ObtenerDuracionMinutos(a));
                        return inicioExistente < finSlot && finExistente > inicioSlot;
                    });

                    if (!hayTraslape)
                    {
                        sugerencias.Add(inicioSlot);
                    }
                }

                inicioSlot = inicioSlot.AddMinutes(30);
            }
        }

        return sugerencias;
    }

    // ── Mapeo a DTO ──

    private static HorarioSemanalDto MapToDto(HorarioSemanal h)
    {
        return new HorarioSemanalDto
        {
            Id = h.Id,
            BarberoId = h.BarberoId,
            BarberoNombre = h.Barbero?.Usuario != null
                ? $"{h.Barbero.Usuario.Nombre} {h.Barbero.Usuario.Apellido}".Trim()
                : null,
            FechaInicioSemana = h.FechaInicioSemana,
            FechaFinSemana = h.FechaFinSemana,
            Estado = h.Estado,
            Detalles = h.Detalles.Select(d => new DetalleHorarioDiaDto
            {
                Id = d.Id,
                HorarioSemanalId = d.HorarioSemanalId,
                DiaSemana = d.DiaSemana,
                HoraInicio = d.HoraInicio.ToString(@"hh\:mm"),
                HoraFin = d.HoraFin.ToString(@"hh\:mm")
            }).ToList()
        };
    }

    // ── CRUD para HorarioSemanal ──

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;
        var baseQ = _context.HorariosSemanales
            .Include(h => h.Detalles)
            .Include(h => h.Barbero).ThenInclude(b => b.Usuario)
            .AsNoTracking()
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(h =>
                (h.Barbero != null && h.Barbero.Usuario != null && (
                    (h.Barbero.Usuario.Nombre != null && h.Barbero.Usuario.Nombre.ToLower().Contains(term)) ||
                    (h.Barbero.Usuario.Apellido != null && h.Barbero.Usuario.Apellido.ToLower().Contains(term))
                )) ||
                (h.Estado != null && h.Estado.ToLower().Contains(term))
            );
        }
        var totalCount = await baseQ.CountAsync();
        var items = await baseQ.OrderByDescending(h => h.FechaInicioSemana).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items = items.Select(MapToDto), totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var horario = await _context.HorariosSemanales
            .AsNoTracking()
            .Include(h => h.Detalles)
            .Include(h => h.Barbero).ThenInclude(b => b.Usuario)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (horario == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(MapToDto(horario));
    }

    public async Task<ServiceResult<object>> GetByBarberoAsync(int barberoId, int page, int pageSize, string? q)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;
        var baseQ = _context.HorariosSemanales
            .Include(h => h.Detalles)
            .Include(h => h.Barbero).ThenInclude(b => b.Usuario)
            .Where(h => h.BarberoId == barberoId)
            .OrderByDescending(h => h.FechaInicioSemana)
            .AsNoTracking()
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(h => h.Estado != null && h.Estado.ToLower().Contains(term));
        }
        var totalCount = await baseQ.CountAsync();
        var items = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items = items.Select(MapToDto), totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> CreateSemanaAsync(HorarioSemanalCreateInput input)
    {
        if (input == null)
            return ServiceResult<object>.Fail("Los datos del horario semanal son requeridos");

        var barbero = await _context.Barberos.FindAsync(input.BarberoId);
        if (barbero == null)
            return ServiceResult<object>.Fail("El barbero especificado no existe");

        if (input.FechaFinSemana <= input.FechaInicioSemana)
            return ServiceResult<object>.Fail("FechaFinSemana debe ser posterior a FechaInicioSemana");

        if (input.Detalles == null || input.Detalles.Count == 0)
            return ServiceResult<object>.Fail("Debe incluir al menos un detalle de día");

        // Verificar que no exista un horario semanal que se traslape para este barbero
        var existeTraslape = await _context.HorariosSemanales.AnyAsync(h =>
            h.BarberoId == input.BarberoId &&
            h.Estado != "Finalizado" &&
            h.FechaInicioSemana <= input.FechaFinSemana.Date &&
            h.FechaFinSemana >= input.FechaInicioSemana.Date);

        if (existeTraslape)
            return ServiceResult<object>.Fail("Ya existe un horario semanal que se traslapa con las fechas indicadas para este barbero.");

        // Validar detalles
        foreach (var d in input.Detalles)
        {
            if (d.DiaSemana < 1 || d.DiaSemana > 7)
                return ServiceResult<object>.Fail($"DiaSemana inválido: {d.DiaSemana}. Debe ser entre 1 (Lunes) y 7 (Domingo).");
            if (d.HoraFin <= d.HoraInicio)
                return ServiceResult<object>.Fail($"HoraFin debe ser mayor que HoraInicio para el día {d.DiaSemana}.");
        }

        // Determinar estado automáticamente
        var hoy = DateTime.Today;
        string estado;
        if (input.FechaInicioSemana.Date <= hoy && input.FechaFinSemana.Date >= hoy)
            estado = "Activo";
        else if (input.FechaInicioSemana.Date > hoy)
            estado = "Pendiente";
        else
            estado = "Finalizado";

        var horarioSemanal = new HorarioSemanal
        {
            BarberoId = input.BarberoId,
            FechaInicioSemana = input.FechaInicioSemana.Date,
            FechaFinSemana = input.FechaFinSemana.Date,
            Estado = estado,
            Detalles = input.Detalles.Select(d => new DetalleHorarioDia
            {
                DiaSemana = d.DiaSemana,
                HoraInicio = d.HoraInicio,
                HoraFin = d.HoraFin
            }).ToList()
        };

        _context.HorariosSemanales.Add(horarioSemanal);
        await _context.SaveChangesAsync();

        await _context.Entry(horarioSemanal).Reference(h => h.Barbero).LoadAsync();
        if (horarioSemanal.Barbero != null)
            await _context.Entry(horarioSemanal.Barbero).Reference(b => b.Usuario).LoadAsync();

        return ServiceResult<object>.Ok(MapToDto(horarioSemanal));
    }

    public async Task<ServiceResult<object>> UpdateSemanaAsync(int id, HorarioSemanalUpdateInput input)
    {
        var horario = await _context.HorariosSemanales
            .Include(h => h.Detalles)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (horario == null) return ServiceResult<object>.NotFound();

        if (input.FechaInicioSemana.HasValue) horario.FechaInicioSemana = input.FechaInicioSemana.Value.Date;
        if (input.FechaFinSemana.HasValue) horario.FechaFinSemana = input.FechaFinSemana.Value.Date;
        if (!string.IsNullOrWhiteSpace(input.Estado)) horario.Estado = input.Estado;

        if (input.Detalles != null)
        {
            // Reemplazar detalles
            _context.DetalleHorarioDias.RemoveRange(horario.Detalles);
            horario.Detalles = input.Detalles.Select(d => new DetalleHorarioDia
            {
                HorarioSemanalId = horario.Id,
                DiaSemana = d.DiaSemana,
                HoraInicio = d.HoraInicio,
                HoraFin = d.HoraFin
            }).ToList();
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.HorariosSemanales.AnyAsync(e => e.Id == id))
                return ServiceResult<object>.NotFound();
            throw;
        }

        await _context.Entry(horario).Reference(h => h.Barbero).LoadAsync();
        if (horario.Barbero != null)
            await _context.Entry(horario.Barbero).Reference(b => b.Usuario).LoadAsync();

        return ServiceResult<object>.Ok(MapToDto(horario));
    }

    public async Task<ServiceResult<object>> DeleteSemanaAsync(int id)
    {
        var horario = await _context.HorariosSemanales
            .Include(h => h.Detalles)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (horario == null) return ServiceResult<object>.NotFound();

        _context.HorariosSemanales.Remove(horario);
        await _context.SaveChangesAsync();

        return ServiceResult<object>.Ok(new
        {
            message = "Horario semanal eliminado exitosamente",
            eliminado = true,
            barberoId = horario.BarberoId
        });
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoHorarioInput input)
    {
        var horarioSemanal = await _context.HorariosSemanales
            .Include(h => h.Detalles)
            .Include(h => h.Barbero).ThenInclude(b => b.Usuario)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (horarioSemanal == null) return ServiceResult<object>.NotFound();

        if (!input.estado)
        {
            // Desactivar → Finalizar
            if (input.UsuarioSolicitanteId <= 0)
                return ServiceResult<object>.Fail("Debe enviar UsuarioSolicitanteId para desactivar horarios.");

            var usuarioSolicitante = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Barbero)
                .FirstOrDefaultAsync(u => u.Id == input.UsuarioSolicitanteId && u.Estado);

            if (usuarioSolicitante == null)
                return ServiceResult<object>.Fail("Usuario solicitante inválido o inactivo.", 401);

            if (!PuedeGestionarDesactivacion(usuarioSolicitante, horarioSemanal.BarberoId))
                return ServiceResult<object>.Fail("No tiene permisos para esta acción.", 403);

            horarioSemanal.Estado = "Finalizado";

            var fechaReferencia = (input.FechaReferencia ?? input.FechaHora ?? DateTime.Today).Date;
            var inicioDia = fechaReferencia;
            var finDia = inicioDia.AddDays(1);
            var motivo = string.IsNullOrWhiteSpace(input.Motivo)
                ? "Horario semanal desactivado por administración."
                : input.Motivo!.Trim();

            var agendamientosAfectados = await _context.Agendamientos
                .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
                .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
                .Include(a => a.Servicio)
                .Include(a => a.Paquete)
                .Where(a => a.BarberoId == horarioSemanal.BarberoId
                            && a.FechaHora >= horarioSemanal.FechaInicioSemana
                            && a.FechaHora <= horarioSemanal.FechaFinSemana.AddDays(1)
                            && (a.Estado == null || (a.Estado != "Cancelada" && a.Estado != "Completada")))
                .OrderBy(a => a.FechaHora)
                .ToListAsync();

            var citasCanceladas = new List<object>();

            foreach (var agendamiento in agendamientosAfectados)
            {
                agendamiento.Estado = "Cancelada";
                agendamiento.Notas = AgregarNotaSistema(agendamiento.Notas, motivo, fechaReferencia);

                citasCanceladas.Add(new
                {
                    citaId = agendamiento.Id,
                    clienteId = agendamiento.ClienteId,
                    clienteNombre = $"{agendamiento.Cliente?.Usuario?.Nombre} {agendamiento.Cliente?.Usuario?.Apellido}".Trim(),
                    clienteCorreo = agendamiento.Cliente?.Usuario?.Correo,
                    barberoId = agendamiento.BarberoId,
                    barberoNombre = $"{agendamiento.Barbero?.Usuario?.Nombre} {agendamiento.Barbero?.Usuario?.Apellido}".Trim(),
                    fechaHoraOriginal = agendamiento.FechaHora,
                    estadoFinal = agendamiento.Estado
                });
            }

            await _context.SaveChangesAsync();

            foreach (var agendamiento in agendamientosAfectados)
            {
                await _notificacionCitasService.NotificarCancelacionPorDesactivacionAsync(
                    agendamiento, motivo, Array.Empty<DateTime>());
            }

            return ServiceResult<object>.Ok(new
            {
                exitoso = true,
                mensaje = "Horario semanal finalizado y citas afectadas canceladas.",
                horarioSemanalId = horarioSemanal.Id,
                barberoId = horarioSemanal.BarberoId,
                citasCanceladas = citasCanceladas.Count,
                detalle = citasCanceladas
            });
        }

        // Activar
        horarioSemanal.Estado = "Activo";
        await _context.SaveChangesAsync();

        return ServiceResult<object>.Ok(new
        {
            entidad = horarioSemanal,
            mensaje = "Horario semanal activado exitosamente",
            exitoso = true
        });
    }

    public async Task<ServiceResult<object>> GetDisponiblesAsync(string fecha)
    {
        if (!DateTime.TryParse(fecha, out DateTime fechaConsulta))
            return ServiceResult<object>.Fail("Formato de fecha inválido");

        var diaSemana = DiaSemanaDominicalANumerico(fechaConsulta.DayOfWeek);
        var fechaDate = fechaConsulta.Date;

        // Buscar todos los HorariosSemanales activos que cubran esa fecha y tengan un detalle para ese día
        var horariosDisponibles = await _context.HorariosSemanales
            .Include(h => h.Detalles)
            .Include(h => h.Barbero).ThenInclude(b => b.Usuario)
            .Where(h => h.Estado == "Activo"
                        && h.FechaInicioSemana <= fechaDate
                        && h.FechaFinSemana >= fechaDate
                        && h.Barbero.Estado == true)
            .AsNoTracking()
            .ToListAsync();

        var result = horariosDisponibles
            .SelectMany(h => h.Detalles
                .Where(d => d.DiaSemana == diaSemana)
                .Select(d => new
                {
                    id = h.Id,
                    barberoId = h.BarberoId,
                    barberoNombre = h.Barbero.Usuario.Nombre + " " + h.Barbero.Usuario.Apellido,
                    diaSemana = d.DiaSemana,
                    horaInicio = d.HoraInicio.ToString(@"hh\:mm"),
                    horaFin = d.HoraFin.ToString(@"hh\:mm")
                }))
            .ToList();

        return ServiceResult<object>.Ok(result);
    }

    public async Task<ServiceResult<object>> CancelarDiaPorBarberoAsync(int barberoId, CambioEstadoHorarioInput input)
    {
        if (input == null) return ServiceResult<object>.Fail("Entrada requerida");
        if (input.UsuarioSolicitanteId <= 0) return ServiceResult<object>.Fail("Debe enviar UsuarioSolicitanteId para cancelar día.");

        var usuarioSolicitante = await _context.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.Barbero)
            .FirstOrDefaultAsync(u => u.Id == input.UsuarioSolicitanteId && u.Estado);

        if (usuarioSolicitante == null)
            return ServiceResult<object>.Fail("Usuario solicitante inválido o inactivo.", 401);

        if (!PuedeGestionarDesactivacion(usuarioSolicitante, barberoId))
            return ServiceResult<object>.Fail("No tiene permisos para esta acción.", 403);

        var fechaReferencia = (input.FechaReferencia ?? input.FechaHora ?? DateTime.Today).Date;
        var inicioDia = fechaReferencia;
        var finDia = inicioDia.AddDays(1);

        var motivo = string.IsNullOrWhiteSpace(input.Motivo)
            ? "Día desactivado por administración."
            : input.Motivo!.Trim();

        var agendamientosAfectados = await _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete)
            .Where(a => a.BarberoId == barberoId
                        && a.FechaHora >= inicioDia
                        && a.FechaHora < finDia
                        && (a.Estado == null || (a.Estado != "Cancelada" && a.Estado != "Completada")))
            .OrderBy(a => a.FechaHora)
            .ToListAsync();

        var citasCanceladas = new List<object>();

        foreach (var agendamiento in agendamientosAfectados)
        {
            agendamiento.Estado = "Cancelada";
            agendamiento.Notas = AgregarNotaSistema(agendamiento.Notas, motivo, fechaReferencia);

            citasCanceladas.Add(new
            {
                citaId = agendamiento.Id,
                clienteId = agendamiento.ClienteId,
                clienteNombre = $"{agendamiento.Cliente?.Usuario?.Nombre} {agendamiento.Cliente?.Usuario?.Apellido}".Trim(),
                clienteCorreo = agendamiento.Cliente?.Usuario?.Correo,
                barberoId = agendamiento.BarberoId,
                barberoNombre = $"{agendamiento.Barbero?.Usuario?.Nombre} {agendamiento.Barbero?.Usuario?.Apellido}".Trim(),
                fechaHoraOriginal = agendamiento.FechaHora,
                estadoFinal = agendamiento.Estado
            });
        }

        await _context.SaveChangesAsync();

        foreach (var agendamiento in agendamientosAfectados)
        {
            await _notificacionCitasService.NotificarCancelacionPorDesactivacionAsync(
                agendamiento, motivo, Array.Empty<DateTime>());
        }

        return ServiceResult<object>.Ok(new
        {
            exitoso = true,
            mensaje = "Día cancelado para el barbero y citas afectadas canceladas.",
            barberoId = barberoId,
            fechaCancelada = fechaReferencia.ToString("yyyy-MM-dd"),
            citasCanceladas = citasCanceladas.Count,
            detalle = citasCanceladas
        });
    }
}
