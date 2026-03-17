using BarberiaApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgendamientosController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public AgendamientosController(BarberiaContext context)
        {
            
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AgendamientoDTO>>> GetAll([FromQuery] string? q = null)
        {
            var limite = DateTime.Now.AddDays(-7);
            var baseQ = _context.Agendamientos
                .Where(a => a.FechaHora >= limite)
                .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
                .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
                .Include(a => a.Servicio)
                .Include(a => a.Paquete)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(a =>
                    (a.Estado != null && a.Estado.ToLower().Contains(term)) ||
                    (a.Cliente != null && a.Cliente.Usuario != null && (
                        (a.Cliente.Usuario.Nombre != null && a.Cliente.Usuario.Nombre.ToLower().Contains(term)) ||
                        (a.Cliente.Usuario.Apellido != null && a.Cliente.Usuario.Apellido.ToLower().Contains(term))
                    )) ||
                    (a.Barbero != null && a.Barbero.Usuario != null && (
                        (a.Barbero.Usuario.Nombre != null && a.Barbero.Usuario.Nombre.ToLower().Contains(term)) ||
                        (a.Barbero.Usuario.Apellido != null && a.Barbero.Usuario.Apellido.ToLower().Contains(term))
                    )) ||
                    (a.Servicio != null && a.Servicio.Nombre != null && a.Servicio.Nombre.ToLower().Contains(term)) ||
                    (a.Paquete != null && a.Paquete.Nombre != null && a.Paquete.Nombre.ToLower().Contains(term))
                );
            }
            var items = await baseQ
                .OrderByDescending(a => a.FechaHora)
                .Select(a => new AgendamientoDTO
                {
                    Id = a.Id,
                    ClienteId = a.ClienteId,
                    BarberoId = a.BarberoId,
                    ServicioId = a.ServicioId,
                    PaqueteId = a.PaqueteId,
                    ClienteNombre = a.Cliente.Usuario.Nombre + " " + a.Cliente.Usuario.Apellido,
                    BarberoNombre = a.Barbero.Usuario.Nombre + " " + a.Barbero.Usuario.Apellido,
                    ServicioNombre = a.Servicio != null ? a.Servicio.Nombre : null,
                    PaqueteNombre = a.Paquete != null ? a.Paquete.Nombre : null,
                    FechaHora = a.FechaHora,
                    Estado = a.Estado,
                    Duracion = a.Duracion,
                    Precio = a.Precio,
                    Notas = a.Notas
                })
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AgendamientoDTO>> GetById(int id)
        {
            var agendamiento = await _context.Agendamientos
                .Include(a => a.Cliente)
                    .ThenInclude(c => c.Usuario)
                .Include(a => a.Barbero)
                    .ThenInclude(b => b.Usuario)
                .Include(a => a.Servicio)
                .Include(a => a.Paquete)
                .Where(a => a.Id == id)
                .Select(a => new AgendamientoDTO
                {
                    Id = a.Id,
                    ClienteId = a.ClienteId,
                    BarberoId = a.BarberoId,
                    ServicioId = a.ServicioId,
                    PaqueteId = a.PaqueteId,
                    ClienteNombre = a.Cliente.Usuario.Nombre + " " + a.Cliente.Usuario.Apellido,
                    BarberoNombre = a.Barbero.Usuario.Nombre + " " + a.Barbero.Usuario.Apellido,
                    ServicioNombre = a.Servicio != null ? a.Servicio.Nombre : null,
                    PaqueteNombre = a.Paquete != null ? a.Paquete.Nombre : null,
                    FechaHora = a.FechaHora,
                    Estado = a.Estado,
                    Duracion = a.Duracion,
                    Precio = a.Precio,
                    Notas = a.Notas
                })
                .FirstOrDefaultAsync();

            if (agendamiento == null) return NotFound();
            return Ok(agendamiento);
        }

        [HttpGet("barbero/{barberoId}/{fecha}")]
        public async Task<ActionResult<IEnumerable<AgendamientoDTO>>> GetByBarberoYFecha(int barberoId, DateTime fecha)
        {
            var inicioDia = fecha.Date;
            var finDia = inicioDia.AddDays(1);

            return await _context.Agendamientos
                .Include(a => a.Cliente)
                    .ThenInclude(c => c.Usuario)
                .Include(a => a.Barbero)
                    .ThenInclude(b => b.Usuario)
                .Include(a => a.Servicio)
                .Include(a => a.Paquete)
                .Where(a => a.BarberoId == barberoId && a.FechaHora >= inicioDia && a.FechaHora < finDia)
                .Select(a => new AgendamientoDTO
                {
                    Id = a.Id,
                    ClienteId = a.ClienteId,
                    BarberoId = a.BarberoId,
                    ServicioId = a.ServicioId,
                    PaqueteId = a.PaqueteId,
                    ClienteNombre = a.Cliente.Usuario.Nombre + " " + a.Cliente.Usuario.Apellido,
                    BarberoNombre = a.Barbero.Usuario.Nombre + " " + a.Barbero.Usuario.Apellido,
                    ServicioNombre = a.Servicio != null ? a.Servicio.Nombre : null,
                    PaqueteNombre = a.Paquete != null ? a.Paquete.Nombre : null,
                    FechaHora = a.FechaHora,
                    Estado = a.Estado,
                    Duracion = a.Duracion,
                    Precio = a.Precio,
                    Notas = a.Notas
                })
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Agendamiento>> Create([FromBody] AgendamientoInput input)
        {
            if (input == null) return BadRequest();

            // Validar que tenga servicio o paquete, no ambos
            if (input.ServicioId.HasValue && input.PaqueteId.HasValue)
                return BadRequest("No se puede agendar un servicio y un paquete simultáneamente.");
            
            if (!input.ServicioId.HasValue && !input.PaqueteId.HasValue)
                return BadRequest("Debe especificar un servicio o un paquete.");

            // Validar disponibilidad
            var duracionMinutos = 30; // Por defecto
            if (input.ServicioId.HasValue)
            {
                var servicio = await _context.Servicios.FindAsync(input.ServicioId.Value);
                if (servicio == null) return BadRequest("Servicio no encontrado.");
                duracionMinutos = servicio.DuracionMinutes ?? 30;
            }
            else if (input.PaqueteId.HasValue)
            {
                var paquete = await _context.Paquetes.FindAsync(input.PaqueteId.Value);
                if (paquete == null) return BadRequest("Paquete no encontrado.");
                duracionMinutos = paquete.DuracionMinutos;
            }

            // Validar que el barbero esté activo y trabaje en el horario
            var barbero = await _context.Barberos.Include(b => b.Usuario).FirstOrDefaultAsync(b => b.Id == input.BarberoId);
            if (barbero == null) return BadRequest("Barbero no encontrado.");
            if (!barbero.Estado || !barbero.Usuario.Estado) return BadRequest("El barbero seleccionado no está activo.");

            var diaSemana = (int)input.FechaHora.DayOfWeek;
            if (diaSemana == 0) diaSemana = 7;

            var horario = await _context.HorariosBarberos
                .FirstOrDefaultAsync(h => h.BarberoId == input.BarberoId && h.DiaSemana == diaSemana && h.Estado == true);

            if (horario == null)
                return BadRequest("El barbero no trabaja en este día.");

            var horaCita = input.FechaHora.TimeOfDay;
            var horaFinCita = horaCita.Add(TimeSpan.FromMinutes(duracionMinutos));

            if (horaCita < horario.HoraInicio || horaFinCita > horario.HoraFin)
                return BadRequest($"La cita está fuera del horario de trabajo del barbero ({horario.HoraInicio:hh\\:mm} - {horario.HoraFin:hh\\:mm}).");

            var horaFin = input.FechaHora.AddMinutes(duracionMinutos);
            
            var existeTraslape = await _context.Agendamientos.AnyAsync(a => 
                a.BarberoId == input.BarberoId && 
                a.FechaHora < horaFin &&
                a.FechaHora.AddMinutes(duracionMinutos) > input.FechaHora &&
                a.Estado != "Cancelada");

            if (existeTraslape)
                return BadRequest("El barbero ya tiene una cita programada en ese horario.");

            var agendamiento = new Agendamiento
            {
                ClienteId = input.ClienteId,
                BarberoId = input.BarberoId,
                ServicioId = input.ServicioId,
                PaqueteId = input.PaqueteId,
                FechaHora = input.FechaHora,
                Notas = input.Notas,
                Duracion = input.Duracion ?? $"{duracionMinutos} minutos",
                Precio = input.Precio ?? (input.ServicioId.HasValue ? 
                    (await _context.Servicios.FindAsync(input.ServicioId.Value))?.Precio :
                    (input.PaqueteId.HasValue ? 
                    (await _context.Paquetes.FindAsync(input.PaqueteId.Value))?.Precio : null)),
                Estado = "Pendiente"
            };

            _context.Agendamientos.Add(agendamiento);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = agendamiento.Id }, new AgendamientoDTO
            {
                Id = agendamiento.Id,
                ClienteId = agendamiento.ClienteId,
                BarberoId = agendamiento.BarberoId,
                ServicioId = agendamiento.ServicioId,
                PaqueteId = agendamiento.PaqueteId,
                ClienteNombre = (await _context.Clientes.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == agendamiento.ClienteId))?.Usuario?.Nombre + " " + (await _context.Clientes.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == agendamiento.ClienteId))?.Usuario?.Apellido,
                BarberoNombre = (await _context.Barberos.Include(b => b.Usuario).FirstOrDefaultAsync(b => b.Id == agendamiento.BarberoId))?.Usuario?.Nombre + " " + (await _context.Barberos.Include(b => b.Usuario).FirstOrDefaultAsync(b => b.Id == agendamiento.BarberoId))?.Usuario?.Apellido,
                ServicioNombre = agendamiento.ServicioId.HasValue ? (await _context.Servicios.FindAsync(agendamiento.ServicioId.Value))?.Nombre : null,
                PaqueteNombre = agendamiento.PaqueteId.HasValue ? (await _context.Paquetes.FindAsync(agendamiento.PaqueteId.Value))?.Nombre : null,
                FechaHora = agendamiento.FechaHora,
                Estado = agendamiento.Estado,
                Duracion = agendamiento.Duracion,
                Precio = agendamiento.Precio,
                Notas = agendamiento.Notas
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AgendamientoInput input)
        {
            var agendamientoExistente = await _context.Agendamientos
                .Include(a => a.Cliente)
                .Include(a => a.Barbero)
                .Include(a => a.Servicio)
                .Include(a => a.Paquete)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (agendamientoExistente == null) return NotFound();

            // Validar que tenga servicio o paquete, no ambos
            if (input.ServicioId.HasValue && input.PaqueteId.HasValue)
                return BadRequest("No se puede agendar un servicio y un paquete simultáneamente.");
            
            if (!input.ServicioId.HasValue && !input.PaqueteId.HasValue)
                return BadRequest("Debe especificar un servicio o un paquete.");

            // Validar duración
            var duracionMinutos = 30; // Por defecto
            if (input.ServicioId.HasValue)
            {
                var servicio = await _context.Servicios.FindAsync(input.ServicioId.Value);
                if (servicio == null) return BadRequest("Servicio no encontrado.");
                duracionMinutos = servicio.DuracionMinutes ?? 30;
            }
            else if (input.PaqueteId.HasValue)
            {
                var paquete = await _context.Paquetes.FindAsync(input.PaqueteId.Value);
                if (paquete == null) return BadRequest("Paquete no encontrado.");
                duracionMinutos = paquete.DuracionMinutos;
            }

            // Validar que el barbero esté activo y trabaje en el horario
            var barberoNuevo = await _context.Barberos.Include(b => b.Usuario).FirstOrDefaultAsync(b => b.Id == input.BarberoId);
            if (barberoNuevo == null) return BadRequest("Barbero no encontrado.");
            if (!barberoNuevo.Estado || !barberoNuevo.Usuario.Estado) return BadRequest("El barbero seleccionado no está activo.");

            var diaSemana = (int)input.FechaHora.DayOfWeek;
            if (diaSemana == 0) diaSemana = 7;

            var horario = await _context.HorariosBarberos
                .FirstOrDefaultAsync(h => h.BarberoId == input.BarberoId && h.DiaSemana == diaSemana && h.Estado == true);

            if (horario == null)
                return BadRequest("El barbero no trabaja en este día.");

            var horaCita = input.FechaHora.TimeOfDay;
            var horaFinCita = horaCita.Add(TimeSpan.FromMinutes(duracionMinutos));

            if (horaCita < horario.HoraInicio || horaFinCita > horario.HoraFin)
                return BadRequest($"La cita está fuera del horario de trabajo del barbero ({horario.HoraInicio:hh\\:mm} - {horario.HoraFin:hh\\:mm}).");

            var horaFin = input.FechaHora.AddMinutes(duracionMinutos);
            var existeTraslape = await _context.Agendamientos.AnyAsync(a => 
                a.Id != id &&
                a.BarberoId == input.BarberoId && 
                a.FechaHora < horaFin &&
                a.FechaHora.AddMinutes(duracionMinutos) > input.FechaHora &&
                a.Estado != "Cancelada");

            if (existeTraslape)
                return BadRequest("El barbero ya tiene una cita programada en ese horario.");

            // Actualizar campos
            agendamientoExistente.ClienteId = input.ClienteId;
            agendamientoExistente.BarberoId = input.BarberoId;
            agendamientoExistente.ServicioId = input.ServicioId;
            agendamientoExistente.PaqueteId = input.PaqueteId;
            agendamientoExistente.FechaHora = input.FechaHora;
            agendamientoExistente.Notas = input.Notas;
            agendamientoExistente.Duracion = input.Duracion ?? agendamientoExistente.Duracion;
            agendamientoExistente.Precio = input.Precio ?? agendamientoExistente.Precio;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Agendamientos.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpPatch("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambioEstadoInput input)
        {
            var agendamiento = await _context.Agendamientos
                .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
                .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
                .Include(a => a.Servicio)
                .Include(a => a.Paquete)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (agendamiento == null) return NotFound();

            var estadosValidos = new[] { "Pendiente", "Confirmada", "En Proceso", "Completada", "Cancelada" };
            if (!estadosValidos.Contains(input.estado))
                return BadRequest("Estado inválido.");

            var estadoAnterior = agendamiento.Estado ?? "Pendiente";
            agendamiento.Estado = input.estado;

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.SaveChangesAsync();

                if (string.Equals(input.estado, "Completada", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(estadoAnterior, "Completada", StringComparison.OrdinalIgnoreCase))
                {
                    var usuarioId = agendamiento.Barbero?.UsuarioId ?? 0;
                    if (usuarioId == 0) return BadRequest("El barbero asociado a la cita no tiene usuario válido.");

                    decimal precio = agendamiento.Precio.HasValue
                        ? agendamiento.Precio.Value
                        : (agendamiento.Servicio != null
                            ? agendamiento.Servicio.Precio
                            : (agendamiento.Paquete != null ? agendamiento.Paquete.Precio : 0m));

                    var ventaExistente = await _context.Ventas
                        .Include(v => v.DetalleVenta)
                        .Where(v => v.ClienteId == agendamiento.ClienteId
                                    && v.UsuarioId == usuarioId)
                        .Where(v => v.DetalleVenta.Any(d =>
                            (agendamiento.ServicioId.HasValue && d.ServicioId == agendamiento.ServicioId) ||
                            (agendamiento.PaqueteId.HasValue && d.PaqueteId == agendamiento.PaqueteId)))
                        .OrderByDescending(v => v.Id)
                        .FirstOrDefaultAsync();
                    if (ventaExistente != null)
                    {
                        if (string.Equals(ventaExistente.Estado, "Anulada", StringComparison.OrdinalIgnoreCase))
                        {
                            ventaExistente.Estado = "Completada";
                            if (!ventaExistente.BarberoId.HasValue || ventaExistente.BarberoId.Value <= 0)
                            {
                                ventaExistente.BarberoId = agendamiento.BarberoId;
                            }
                            ventaExistente.Fecha = agendamiento.FechaHora;
                            // Asegurar detalle mínimo si no existe por algún motivo
                            if (!ventaExistente.DetalleVenta.Any())
                            {
                                var detalleReactivado = new DetalleVenta
                                {
                                    VentaId = ventaExistente.Id,
                                    ServicioId = agendamiento.ServicioId,
                                    PaqueteId = agendamiento.PaqueteId,
                                    Cantidad = 1,
                                    PrecioUnitario = precio
                                };
                                _context.DetalleVentas.Add(detalleReactivado);
                            }
                            await _context.SaveChangesAsync();
                            await tx.CommitAsync();
                            return Ok(new {
                                message = "Estado actualizado. Venta existente reactivada",
                                estadoActual = input.estado,
                                agendamientoId = id,
                                ventaId = ventaExistente.Id,
                                venta = new {
                                    Id = ventaExistente.Id,
                                    ClienteId = ventaExistente.ClienteId,
                                    UsuarioId = ventaExistente.UsuarioId,
                                    BarberoId = ventaExistente.BarberoId,
                                    Fecha = ventaExistente.Fecha,
                                    Subtotal = ventaExistente.Subtotal,
                                    Total = ventaExistente.Total,
                                    Estado = ventaExistente.Estado,
                                    MetodoPago = ventaExistente.MetodoPago
                                }
                            });
                        }
                        else
                        {
                            await tx.CommitAsync();
                            return Ok(new {
                                message = "Estado actualizado. Venta ya existente no duplicada",
                                estadoActual = input.estado,
                                agendamientoId = id,
                                ventaId = ventaExistente.Id,
                                venta = new {
                                    Id = ventaExistente.Id,
                                    ClienteId = ventaExistente.ClienteId,
                                    UsuarioId = ventaExistente.UsuarioId,
                                    BarberoId = ventaExistente.BarberoId,
                                    Fecha = ventaExistente.Fecha,
                                    Subtotal = ventaExistente.Subtotal,
                                    Total = ventaExistente.Total,
                                    Estado = ventaExistente.Estado,
                                    MetodoPago = ventaExistente.MetodoPago
                                }
                            });
                        }
                    }

                    var venta = new Venta
                    {
                        UsuarioId = usuarioId,
                        ClienteId = agendamiento.ClienteId,
                        BarberoId = agendamiento.BarberoId,
                        Fecha = agendamiento.FechaHora,
                        Subtotal = precio,
                        IVA = 0m,
                        Descuento = 0m,
                        Total = precio,
                        MetodoPago = "Efectivo",
                        Estado = "Completada",
                        SaldoAFavorUsado = 0m
                    };

                    _context.Ventas.Add(venta);
                    await _context.SaveChangesAsync();

                    var detalle = new DetalleVenta
                    {
                        VentaId = venta.Id,
                        ServicioId = agendamiento.ServicioId,
                        PaqueteId = agendamiento.PaqueteId,
                        Cantidad = 1,
                        PrecioUnitario = precio
                    };

                    _context.DetalleVentas.Add(detalle);
                    await _context.SaveChangesAsync();
                }
                if (string.Equals(input.estado, "Cancelada", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(estadoAnterior, "Cancelada", StringComparison.OrdinalIgnoreCase))
                {
                    var usuarioId = agendamiento.Barbero?.UsuarioId ?? 0;
                    var ventaRelacionada = await _context.Ventas
                        .Include(v => v.DetalleVenta)
                        .Where(v => v.ClienteId == agendamiento.ClienteId
                                    && v.UsuarioId == usuarioId
                                    && v.Estado != "Anulada")
                        .Where(v => v.DetalleVenta.Any(d =>
                            (agendamiento.ServicioId.HasValue && d.ServicioId == agendamiento.ServicioId) ||
                            (agendamiento.PaqueteId.HasValue && d.PaqueteId == agendamiento.PaqueteId)))
                        .OrderByDescending(v => v.Id)
                        .FirstOrDefaultAsync();
                    if (ventaRelacionada != null)
                    {
                        ventaRelacionada.Estado = "Anulada";
                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();
                        return Ok(new {
                            message = "Estado actualizado. Venta asociada anulada",
                            estadoActual = input.estado,
                            agendamientoId = id,
                            ventaId = ventaRelacionada.Id,
                            venta = new {
                                Id = ventaRelacionada.Id,
                                ClienteId = ventaRelacionada.ClienteId,
                                UsuarioId = ventaRelacionada.UsuarioId,
                                BarberoId = ventaRelacionada.BarberoId,
                                Fecha = ventaRelacionada.Fecha,
                                Subtotal = ventaRelacionada.Subtotal,
                                Total = ventaRelacionada.Total,
                                Estado = ventaRelacionada.Estado,
                                MetodoPago = ventaRelacionada.MetodoPago
                            }
                        });
                    }
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Error al actualizar estado y generar venta: {ex.Message}");
            }

            return Ok(new {
                message = "Estado actualizado correctamente",
                estadoActual = input.estado,
                agendamientoId = id
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var agendamiento = await _context.Agendamientos
                .Include(a => a.Cliente)
                .Include(a => a.Barbero)
                .Include(a => a.Servicio)
                .Include(a => a.Paquete)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (agendamiento == null) return NotFound();

            // Guardar información para la respuesta
            var infoRespuesta = new {
                message = "Agendamiento eliminado permanentemente",
                eliminado = true,
                fisico = true,
                clienteAsociado = agendamiento.Cliente?.Usuario?.Nombre + " " + agendamiento.Cliente?.Usuario?.Apellido,
                barberoAsociado = agendamiento.Barbero?.Usuario?.Nombre + " " + agendamiento.Barbero?.Usuario?.Apellido,
                servicioAsociado = agendamiento.Servicio?.Nombre,
                paqueteAsociado = agendamiento.Paquete?.Nombre
            };

            // Borrar físicamente el agendamiento
            _context.Agendamientos.Remove(agendamiento);
            await _context.SaveChangesAsync();
            
            return Ok(infoRespuesta);
        }
    }
}
