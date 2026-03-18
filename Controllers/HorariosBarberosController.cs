using BarberiaApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.OutputCaching;
using BarberiaApi.Services;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HorariosBarberosController : ControllerBase
    {
        private readonly BarberiaContext _context;
        private readonly INotificacionCitasService _notificacionCitasService;

        public HorariosBarberosController(BarberiaContext context, INotificacionCitasService notificacionCitasService)
        {
            _context = context;
            _notificacionCitasService = notificacionCitasService;
        }

        [HttpGet]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.HorariosBarberos
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
                    ))
                );
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("{id}")]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<HorariosBarbero>> GetById(int id)
        {
            var horario = await _context.HorariosBarberos
                .AsNoTracking()
                .Include(h => h.Barbero)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (horario == null) return NotFound();
            return Ok(horario);
        }

        [HttpGet("barbero/{barberoId}")]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<object>> GetByBarbero(int barberoId, [FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.HorariosBarberos
                .Include(h => h.Barbero)
                .Where(h => h.BarberoId == barberoId && h.Estado == true)
                .OrderBy(h => h.DiaSemana)
                .AsNoTracking()
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(h => h.Barbero != null && h.Barbero.Usuario != null &&
                    ((h.Barbero.Usuario.Nombre != null && h.Barbero.Usuario.Nombre.ToLower().Contains(term)) ||
                     (h.Barbero.Usuario.Apellido != null && h.Barbero.Usuario.Apellido.ToLower().Contains(term))));
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpPost]
        public async Task<ActionResult<HorariosBarbero>> Create([FromBody] HorarioBarberoCreateInput input)
        {
            if (input == null)
                return BadRequest("Los datos del horario son requeridos");

            // Validar que el barbero exista
            var barbero = await _context.Barberos.FindAsync(input.BarberoId);
            if (barbero == null)
                return BadRequest("El barbero especificado no existe");

            // Validar que no exista un horario para el mismo día y barbero
            var horarioExistente = await _context.HorariosBarberos
                .FirstOrDefaultAsync(h => h.BarberoId == input.BarberoId && h.DiaSemana == input.DiaSemana);

            if (horarioExistente != null)
                return BadRequest("Ya existe un horario para este barbero en el día especificado");

            var horario = new HorariosBarbero
            {
                BarberoId = input.BarberoId,
                DiaSemana = input.DiaSemana,
                HoraInicio = input.HoraInicio,
                HoraFin = input.HoraFin,
                Estado = true
            };

            _context.HorariosBarberos.Add(horario);
            await _context.SaveChangesAsync();

            // Retornar el horario creado con el barbero incluido
            await _context.Entry(horario).Reference(h => h.Barbero).LoadAsync();

            return CreatedAtAction(nameof(GetById), new { id = horario.Id }, horario);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<HorariosBarbero>> Update(int id, [FromBody] HorarioBarberoUpdateInput input)
        {
            var horario = await _context.HorariosBarberos.FindAsync(id);
            if (horario == null) return NotFound();

            // Validar que el barbero exista si se está cambiando
            if (input.BarberoId.HasValue)
            {
                var barbero = await _context.Barberos.FindAsync(input.BarberoId.Value);
                if (barbero == null)
                    return BadRequest("El barbero especificado no existe");

                // Validar que no exista un horario para el mismo día y barbero (si cambia el día o el barbero)
                if ((input.DiaSemana.HasValue && input.DiaSemana.Value != horario.DiaSemana) ||
                    (input.BarberoId.HasValue && input.BarberoId.Value != horario.BarberoId))
                {
                    var horarioExistente = await _context.HorariosBarberos
                        .FirstOrDefaultAsync(h => h.BarberoId == input.BarberoId.Value && 
                                               h.DiaSemana == (input.DiaSemana ?? horario.DiaSemana) &&
                                               h.Id != id);

                    if (horarioExistente != null)
                        return BadRequest("Ya existe un horario para este barbero en el día especificado");
                }
            }

            // Actualizar solo los campos proporcionados
            if (input.BarberoId.HasValue) horario.BarberoId = input.BarberoId.Value;
            if (input.DiaSemana.HasValue) horario.DiaSemana = input.DiaSemana.Value;
            if (input.HoraInicio.HasValue) horario.HoraInicio = input.HoraInicio.Value;
            if (input.HoraFin.HasValue) horario.HoraFin = input.HoraFin.Value;
            if (input.Estado.HasValue) horario.Estado = input.Estado.Value;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.HorariosBarberos.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }

            // Retornar el horario actualizado con el barbero incluido
            await _context.Entry(horario).Reference(h => h.Barbero).LoadAsync();

            return Ok(horario);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var horario = await _context.HorariosBarberos.FindAsync(id);
            if (horario == null) return NotFound();

            _context.HorariosBarberos.Remove(horario);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Horario eliminado exitosamente", 
                eliminado = true,
                barberoId = horario.BarberoId,
                diaSemana = horario.DiaSemana
            });
        }

        [HttpPost("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambioEstadoHorarioInput input)
        {
            var horario = await _context.HorariosBarberos
                .Include(h => h.Barbero)
                    .ThenInclude(b => b.Usuario)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (horario == null) return NotFound();

            if (!input.estado)
            {
                if (input.UsuarioSolicitanteId <= 0)
                    return BadRequest("Debe enviar UsuarioSolicitanteId para desactivar horarios.");

                var usuarioSolicitante = await _context.Usuarios
                    .Include(u => u.Rol)
                    .Include(u => u.Barbero)
                    .FirstOrDefaultAsync(u => u.Id == input.UsuarioSolicitanteId && u.Estado);

                if (usuarioSolicitante == null)
                    return Unauthorized("Usuario solicitante inválido o inactivo.");

                if (!PuedeGestionarDesactivacion(usuarioSolicitante, horario))
                    return Forbid();

                horario.Estado = false;

                var fechaReferencia = (input.FechaReferencia ?? DateTime.Today).Date;
                if (DiaSemanaDominicalANumerico(fechaReferencia.DayOfWeek) != horario.DiaSemana)
                    return BadRequest("La FechaReferencia no corresponde al día de semana del horario seleccionado.");

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
                    .Where(a => a.BarberoId == horario.BarberoId
                                && a.FechaHora >= inicioDia
                                && a.FechaHora < finDia
                                && (a.Estado == null || (a.Estado != "Cancelada" && a.Estado != "Completada")))
                    .OrderBy(a => a.FechaHora)
                    .ToListAsync();

                var cantidadSugerencias = input.CantidadSugerencias <= 0 ? 3 : input.CantidadSugerencias;
                var inicioBusquedaSugerencias = fechaReferencia.AddDays(1);
                var finBusquedaSugerencias = inicioBusquedaSugerencias.AddDays(31);
                var horariosActivos = await _context.HorariosBarberos
                    .AsNoTracking()
                    .Where(h => h.BarberoId == horario.BarberoId && h.Estado == true)
                    .ToListAsync();
                var agendaRango = await _context.Agendamientos
                    .AsNoTracking()
                    .Include(a => a.Servicio)
                    .Include(a => a.Paquete)
                    .Where(a => a.BarberoId == horario.BarberoId
                                && a.FechaHora >= inicioBusquedaSugerencias
                                && a.FechaHora < finBusquedaSugerencias
                                && (a.Estado == null || a.Estado != "Cancelada"))
                    .ToListAsync();
                var agendaPorDia = agendaRango
                    .GroupBy(a => a.FechaHora.Date)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var citasCanceladas = new List<object>();
                var correosEnviados = 0;
                var correosFallidos = 0;

                // Preparar las tareas de notificación para ejecución en paralelo
                var tareasNotificacion = agendamientosAfectados.Select(async agendamiento =>
                {
                    var duracion = ObtenerDuracionMinutos(agendamiento);
                    var sugerencias = ObtenerSugerenciasReprogramacionConCache(
                        agendamiento.Id,
                        agendamiento.FechaHora,
                        duracion,
                        cantidadSugerencias,
                        horariosActivos,
                        agendaPorDia);

                    var notificacion = await _notificacionCitasService.NotificarCancelacionPorDesactivacionAsync(
                        agendamiento,
                        motivo,
                        sugerencias);

                    return new { agendamiento, notificacion, sugerencias };
                }).ToList();

                var resultados = await Task.WhenAll(tareasNotificacion);

                foreach (var res in resultados)
                {
                    var agendamiento = res.agendamiento;
                    var notificacion = res.notificacion;
                    var sugerencias = res.sugerencias;

                    if (notificacion.Enviado) correosEnviados++;
                    else correosFallidos++;

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
                        estadoFinal = agendamiento.Estado,
                        notificacion = new
                        {
                            enviado = notificacion.Enviado,
                            canal = notificacion.Canal,
                            mensaje = notificacion.Mensaje
                        },
                        sugerenciasReprogramacion = sugerencias.Select(s => s.ToString("yyyy-MM-ddTHH:mm:ss"))
                    });
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    exitoso = true,
                    mensaje = "Horario desactivado y citas afectadas canceladas.",
                    horarioId = horario.Id,
                    barberoId = horario.BarberoId,
                    fechaDesactivada = fechaReferencia.ToString("yyyy-MM-dd"),
                    citasCanceladas = citasCanceladas.Count,
                    detalle = citasCanceladas,
                    integracionCorreo = new
                    {
                        activa = correosEnviados > 0,
                        estado = correosFallidos == 0 ? "correo_enviado" : "correo_parcial_o_fallido",
                        enviados = correosEnviados,
                        fallidos = correosFallidos
                    }
                });
            }

            horario.Estado = input.estado;
            await _context.SaveChangesAsync();

            var response = new CambioEstadoResponse<HorariosBarbero>
            {
                entidad = horario,
                mensaje = input.estado ? "Horario activado exitosamente" : "Horario desactivado exitosamente",
                exitoso = true
            };

            return Ok(response);
        }

        private static int DiaSemanaDominicalANumerico(DayOfWeek dayOfWeek)
        {
            var dia = (int)dayOfWeek;
            return dia == 0 ? 7 : dia;
        }

        private static bool PuedeGestionarDesactivacion(Usuario usuarioSolicitante, HorariosBarbero horario)
        {
            var nombreRol = (usuarioSolicitante.Rol?.Nombre ?? string.Empty).Trim().ToLowerInvariant();
            var rolId = usuarioSolicitante.RolId ?? 0;

            var esSuperAdministrador = rolId == 18 || (nombreRol.Contains("super") && nombreRol.Contains("admin"));
            var esAdministrador = rolId == 1 || (nombreRol.Contains("admin") && !nombreRol.Contains("barbero") && !nombreRol.Contains("super"));
            var esBarbero = rolId == 2 || nombreRol.Contains("barbero");

            if (esSuperAdministrador || esAdministrador) return true;
            if (esBarbero)
            {
                return usuarioSolicitante.Barbero != null && usuarioSolicitante.Barbero.Id == horario.BarberoId;
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

            if (agendamiento.Servicio?.DuracionMinutes.HasValue == true && agendamiento.Servicio.DuracionMinutes.Value > 0)
            {
                return agendamiento.Servicio.DuracionMinutes.Value;
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

        private async Task<List<DateTime>> ObtenerSugerenciasReprogramacionAsync(
            int barberoId,
            int agendamientoIdActual,
            DateTime fechaOriginal,
            int duracionMinutos,
            int cantidadSugerencias)
        {
            var sugerencias = new List<DateTime>();
            var fechaInicioBusqueda = fechaOriginal.Date.AddDays(1);
            var fechaLimite = fechaInicioBusqueda.AddDays(30);

            var horariosActivos = await _context.HorariosBarberos
                .Where(h => h.BarberoId == barberoId && h.Estado == true)
                .ToListAsync();

            if (horariosActivos.Count == 0) return sugerencias;

            for (var fecha = fechaInicioBusqueda; fecha <= fechaLimite && sugerencias.Count < cantidadSugerencias; fecha = fecha.AddDays(1))
            {
                var diaSemana = DiaSemanaDominicalANumerico(fecha.DayOfWeek);
                var horarioDia = horariosActivos.FirstOrDefault(h => h.DiaSemana == diaSemana);
                if (horarioDia == null) continue;

                var agendamientosDia = await _context.Agendamientos
                    .Include(a => a.Servicio)
                    .Include(a => a.Paquete)
                    .Where(a => a.BarberoId == barberoId
                                && a.Id != agendamientoIdActual
                                && a.FechaHora >= fecha
                                && a.FechaHora < fecha.AddDays(1)
                                && (a.Estado == null || a.Estado != "Cancelada"))
                    .ToListAsync();

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

        [HttpGet("disponibles/{fecha}")]
        public async Task<ActionResult<IEnumerable<object>>> GetDisponibles(string fecha)
        {
            if (!DateTime.TryParse(fecha, out DateTime fechaConsulta))
                return BadRequest("Formato de fecha inválido");

            var diaSemana = (int)fechaConsulta.DayOfWeek;
            // Ajustar para que Lunes = 1, Domingo = 7 (en .NET Sunday = 0)
            if (diaSemana == 0) diaSemana = 7;

            var horariosDisponibles = await _context.HorariosBarberos
                .Include(h => h.Barbero)
                    .ThenInclude(b => b.Usuario)
                .Where(h => h.Estado == true && 
                           h.DiaSemana == diaSemana &&
                           h.Barbero.Estado == true)
                .Select(h => new
                {
                    id = h.Id,
                    barberoId = h.BarberoId,
                    barberoNombre = h.Barbero.Usuario.Nombre + " " + h.Barbero.Usuario.Apellido,
                    diaSemana = h.DiaSemana,
                    horaInicio = h.HoraInicio.ToString(@"hh\:mm"),
                    horaFin = h.HoraFin.ToString(@"hh\:mm")
                })
                .ToListAsync();

            return Ok(horariosDisponibles);
        }

        [HttpPost("barbero/{barberoId}/cancelar-dia")]
        public async Task<IActionResult> CancelarDiaPorBarbero(int barberoId, [FromBody] CambioEstadoHorarioInput input)
        {
            if (input == null) return BadRequest("Entrada requerida");
            if (input.UsuarioSolicitanteId <= 0) return BadRequest("Debe enviar UsuarioSolicitanteId para cancelar día.");

            var usuarioSolicitante = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Barbero)
                .FirstOrDefaultAsync(u => u.Id == input.UsuarioSolicitanteId && u.Estado);

            if (usuarioSolicitante == null)
                return Unauthorized("Usuario solicitante inválido o inactivo.");

            var puedeGestionar = PuedeGestionarDesactivacionPorBarbero(usuarioSolicitante, barberoId);
            if (!puedeGestionar) return Forbid();

            var fechaReferencia = (input.FechaReferencia ?? DateTime.Today).Date;
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

            var cantidadSugerencias = input.CantidadSugerencias <= 0 ? 3 : input.CantidadSugerencias;
            var inicioBusqueda = fechaReferencia.AddDays(1);
            var finBusqueda = inicioBusqueda.AddDays(31);
            var horariosActivos = await _context.HorariosBarberos
                .AsNoTracking()
                .Where(h => h.BarberoId == barberoId && h.Estado == true)
                .ToListAsync();
            var agendaRango = await _context.Agendamientos
                .AsNoTracking()
                .Include(a => a.Servicio)
                .Include(a => a.Paquete)
                .Where(a => a.BarberoId == barberoId
                            && a.FechaHora >= inicioBusqueda
                            && a.FechaHora < finBusqueda
                            && (a.Estado == null || a.Estado != "Cancelada"))
                .ToListAsync();
            var agendaPorDia = agendaRango
                .GroupBy(a => a.FechaHora.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            var citasCanceladas = new List<object>();
            var correosEnviados = 0;
            var correosFallidos = 0;

            // Preparar las tareas de notificación para ejecución en paralelo
            var tareasNotificacion = agendamientosAfectados.Select(async agendamiento =>
            {
                var duracion = ObtenerDuracionMinutos(agendamiento);
                var sugerencias = ObtenerSugerenciasReprogramacionConCache(
                    agendamiento.Id,
                    agendamiento.FechaHora,
                    duracion,
                    cantidadSugerencias,
                    horariosActivos,
                    agendaPorDia);

                var notificacion = await _notificacionCitasService.NotificarCancelacionPorDesactivacionAsync(
                    agendamiento,
                    motivo,
                    sugerencias);

                return new { agendamiento, notificacion, sugerencias };
            }).ToList();

            var resultados = await Task.WhenAll(tareasNotificacion);

            foreach (var res in resultados)
            {
                var agendamiento = res.agendamiento;
                var notificacion = res.notificacion;
                var sugerencias = res.sugerencias;

                if (notificacion.Enviado) correosEnviados++;
                else correosFallidos++;

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
                    estadoFinal = agendamiento.Estado,
                    notificacion = new
                    {
                        enviado = notificacion.Enviado,
                        canal = notificacion.Canal,
                        mensaje = notificacion.Mensaje
                    },
                    sugerenciasReprogramacion = sugerencias.Select(s => s.ToString("yyyy-MM-ddTHH:mm:ss"))
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                exitoso = true,
                mensaje = "Día cancelado para el barbero y citas afectadas canceladas.",
                barberoId = barberoId,
                fechaCancelada = fechaReferencia.ToString("yyyy-MM-dd"),
                citasCanceladas = citasCanceladas.Count,
                detalle = citasCanceladas,
                integracionCorreo = new
                {
                    activa = correosEnviados > 0,
                    estado = correosFallidos == 0 ? "correo_enviado" : "correo_parcial_o_fallido",
                    enviados = correosEnviados,
                    fallidos = correosFallidos
                }
            });
        }

        private static bool PuedeGestionarDesactivacionPorBarbero(Usuario usuarioSolicitante, int barberoId)
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

        private List<DateTime> ObtenerSugerenciasReprogramacionConCache(
            int agendamientoIdActual,
            DateTime fechaOriginal,
            int duracionMinutos,
            int cantidadSugerencias,
            List<HorariosBarbero> horariosActivos,
            Dictionary<DateTime, List<Agendamiento>> agendaPorDia)
        {
            var sugerencias = new List<DateTime>();
            if (horariosActivos == null || horariosActivos.Count == 0) return sugerencias;

            var fechaInicioBusqueda = fechaOriginal.Date.AddDays(1);
            var fechaLimite = fechaInicioBusqueda.AddDays(30);

            for (var fecha = fechaInicioBusqueda; fecha <= fechaLimite && sugerencias.Count < cantidadSugerencias; fecha = fecha.AddDays(1))
            {
                var diaSemana = DiaSemanaDominicalANumerico(fecha.DayOfWeek);
                var horarioDia = horariosActivos.FirstOrDefault(h => h.DiaSemana == diaSemana);
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
    }

    // DTOs para HorariosBarbero
    public class HorarioBarberoCreateInput
    {
        public int BarberoId { get; set; }
        public int DiaSemana { get; set; } // 1=Lunes, 7=Domingo
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
    }

    public class HorarioBarberoUpdateInput
    {
        public int? BarberoId { get; set; }
        public int? DiaSemana { get; set; }
        public TimeSpan? HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }
        public bool? Estado { get; set; }
    }

    public class CambioEstadoHorarioInput
    {
        public bool estado { get; set; }
        public int UsuarioSolicitanteId { get; set; }
        public DateTime? FechaReferencia { get; set; }
        public string? Motivo { get; set; }
        public int CantidadSugerencias { get; set; } = 3;
    }
}
