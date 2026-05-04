using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class SolicitudCambioHorarioService : ISolicitudCambioHorarioService
{
    private readonly BarberiaContext _context;

    public SolicitudCambioHorarioService(BarberiaContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<object>> GetAllAsync(string? estado, int? barberoId, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _context.SolicitudesCambioHorario
            .AsNoTracking()
            .Include(s => s.Barbero).ThenInclude(b => b.Usuario)
            .Include(s => s.Sugerencias)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(s => s.Estado == estado);
        if (barberoId.HasValue)
            query = query.Where(s => s.BarberoId == barberoId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.FechaCreacion)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.BarberoId,
                BarberoNombre = s.Barbero.Usuario != null
                    ? s.Barbero.Usuario.Nombre + " " + s.Barbero.Usuario.Apellido
                    : "Barbero",
                s.MotivoCategoria,
                s.MotivoDetalle,
                s.FechaReferencia,
                s.Estado,
                s.ObservacionAdmin,
                s.FechaCreacion,
                s.FechaResolucion,
                Sugerencias = s.Sugerencias.Select(g => new
                {
                    g.Id,
                    g.DiaSugerido,
                    HoraInicio = g.HoraInicio.ToString(@"hh\:mm"),
                    HoraFin = g.HoraFin.ToString(@"hh\:mm"),
                    g.Origen
                })
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var s = await _context.SolicitudesCambioHorario
            .AsNoTracking()
            .Include(x => x.Barbero).ThenInclude(b => b.Usuario)
            .Include(x => x.UsuarioResolucion)
            .Include(x => x.Sugerencias)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (s == null) return ServiceResult<object>.NotFound();

        return ServiceResult<object>.Ok(new
        {
            s.Id,
            s.BarberoId,
            BarberoNombre = s.Barbero.Usuario != null
                ? s.Barbero.Usuario.Nombre + " " + s.Barbero.Usuario.Apellido
                : "Barbero",
            s.MotivoCategoria,
            s.MotivoDetalle,
            s.FechaReferencia,
            s.Estado,
            s.ObservacionAdmin,
            s.FechaCreacion,
            s.FechaResolucion,
            UsuarioResolucionNombre = s.UsuarioResolucion != null
                ? s.UsuarioResolucion.Nombre + " " + s.UsuarioResolucion.Apellido
                : null,
            Sugerencias = s.Sugerencias.Select(g => new
            {
                g.Id,
                g.DiaSugerido,
                HoraInicio = g.HoraInicio.ToString(@"hh\:mm"),
                HoraFin = g.HoraFin.ToString(@"hh\:mm"),
                g.Origen
            })
        });
    }

    public async Task<ServiceResult<object>> CreateAsync(SolicitudCambioHorarioCreateInput input)
    {
        var barberoExiste = await _context.Barberos.AnyAsync(b => b.Id == input.BarberoId);
        if (!barberoExiste) return ServiceResult<object>.Fail("El barbero no existe");

        if (input.Sugerencias == null || input.Sugerencias.Count == 0)
            return ServiceResult<object>.Fail("Debe enviar al menos una sugerencia de horario");

        foreach (var sug in input.Sugerencias)
        {
            if (sug.HoraFin <= sug.HoraInicio)
                return ServiceResult<object>.Fail("HoraFin debe ser mayor que HoraInicio en cada sugerencia");
        }

        var solicitud = new SolicitudCambioHorario
        {
            BarberoId = input.BarberoId,
            MotivoCategoria = input.MotivoCategoria,
            MotivoDetalle = input.MotivoDetalle,
            FechaReferencia = input.FechaReferencia,
            Estado = "Pendiente",
            FechaCreacion = DateTime.Now,
            Sugerencias = input.Sugerencias.Select(s => new SugerenciaCambioHorario
            {
                DiaSugerido = s.DiaSugerido,
                HoraInicio = s.HoraInicio,
                HoraFin = s.HoraFin,
                Origen = "Barbero"
            }).ToList()
        };

        _context.SolicitudesCambioHorario.Add(solicitud);
        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { solicitud.Id, solicitud.Estado });
    }

    public async Task<ServiceResult<object>> AprobarAsync(int id, int usuarioId)
    {
        var solicitud = await _context.SolicitudesCambioHorario
            .Include(s => s.Sugerencias)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (solicitud == null) return ServiceResult<object>.NotFound();
        if (solicitud.Estado != "Pendiente" && solicitud.Estado != "Sugerida")
            return ServiceResult<object>.Fail($"No se puede aprobar una solicitud en estado '{solicitud.Estado}'", 409);

        solicitud.Estado = "Aprobada";
        solicitud.FechaResolucion = DateTime.Now;
        solicitud.UsuarioResolucionId = usuarioId;

        // Aplicar las sugerencias al horario del barbero
        foreach (var sug in solicitud.Sugerencias)
        {
            var diaSemana = (int)sug.DiaSugerido.DayOfWeek;
            var horario = await _context.HorariosBarberos
                .FirstOrDefaultAsync(h => h.BarberoId == solicitud.BarberoId && h.DiaSemana == diaSemana);

            if (horario == null)
            {
                _context.HorariosBarberos.Add(new HorariosBarbero
                {
                    BarberoId = solicitud.BarberoId,
                    DiaSemana = diaSemana,
                    HoraInicio = sug.HoraInicio,
                    HoraFin = sug.HoraFin,
                    Estado = true
                });
            }
            else
            {
                horario.HoraInicio = sug.HoraInicio;
                horario.HoraFin = sug.HoraFin;
                horario.Estado = true;
            }
        }

        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { solicitud.Id, solicitud.Estado });
    }

    public async Task<ServiceResult<object>> RechazarAsync(int id, int usuarioId, SolicitudCambioHorarioRechazarInput input)
    {
        var solicitud = await _context.SolicitudesCambioHorario
            .Include(s => s.Sugerencias)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (solicitud == null) return ServiceResult<object>.NotFound();
        if (solicitud.Estado != "Pendiente" && solicitud.Estado != "Sugerida")
            return ServiceResult<object>.Fail($"No se puede rechazar una solicitud en estado '{solicitud.Estado}'", 409);

        solicitud.ObservacionAdmin = input.Observacion;
        solicitud.UsuarioResolucionId = usuarioId;

        if (input.Sugerencias != null && input.Sugerencias.Count > 0)
        {
            // Contrapropuesta: estado pasa a "Sugerida" para que el barbero responda
            solicitud.Estado = "Sugerida";

            // Limpiar sugerencias anteriores del admin
            var antiguasAdmin = solicitud.Sugerencias.Where(s => s.Origen == "Admin").ToList();
            _context.SugerenciasCambioHorario.RemoveRange(antiguasAdmin);

            foreach (var sug in input.Sugerencias)
            {
                solicitud.Sugerencias.Add(new SugerenciaCambioHorario
                {
                    DiaSugerido = sug.DiaSugerido,
                    HoraInicio = sug.HoraInicio,
                    HoraFin = sug.HoraFin,
                    Origen = "Admin"
                });
            }
        }
        else
        {
            solicitud.Estado = "Rechazada";
            solicitud.FechaResolucion = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { solicitud.Id, solicitud.Estado });
    }

    public async Task<ServiceResult<object>> ResponderSugerenciaAsync(int id, SolicitudCambioHorarioRespuestaInput input)
    {
        var solicitud = await _context.SolicitudesCambioHorario
            .Include(s => s.Sugerencias)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (solicitud == null) return ServiceResult<object>.NotFound();
        if (solicitud.Estado != "Sugerida")
            return ServiceResult<object>.Fail($"Solo se puede responder a solicitudes en estado 'Sugerida' (estado actual: '{solicitud.Estado}')", 409);

        if (input.Acepta)
        {
            solicitud.Estado = "Aprobada";
            solicitud.FechaResolucion = DateTime.Now;

            // Aplicar sugerencias del admin al horario
            var sugerenciasAdmin = solicitud.Sugerencias.Where(s => s.Origen == "Admin").ToList();
            foreach (var sug in sugerenciasAdmin)
            {
                var diaSemana = (int)sug.DiaSugerido.DayOfWeek;
                var horario = await _context.HorariosBarberos
                    .FirstOrDefaultAsync(h => h.BarberoId == solicitud.BarberoId && h.DiaSemana == diaSemana);
                if (horario == null)
                {
                    _context.HorariosBarberos.Add(new HorariosBarbero
                    {
                        BarberoId = solicitud.BarberoId,
                        DiaSemana = diaSemana,
                        HoraInicio = sug.HoraInicio,
                        HoraFin = sug.HoraFin,
                        Estado = true
                    });
                }
                else
                {
                    horario.HoraInicio = sug.HoraInicio;
                    horario.HoraFin = sug.HoraFin;
                    horario.Estado = true;
                }
            }
        }
        else
        {
            solicitud.Estado = "Rechazada";
            solicitud.FechaResolucion = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(input.Observacion))
                solicitud.ObservacionAdmin = (solicitud.ObservacionAdmin ?? "") + " | Barbero rechazó: " + input.Observacion;
        }

        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { solicitud.Id, solicitud.Estado });
    }
}
