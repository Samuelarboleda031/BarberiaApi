using BarberiaApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaquetesController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public PaquetesController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.Paquetes
                .Include(p => p.DetallePaquetes)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(p =>
                    (p.Nombre != null && p.Nombre.ToLower().Contains(term)) ||
                    (p.Descripcion != null && p.Descripcion.ToLower().Contains(term))
                );
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ
                .OrderBy(p => p.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Paquete>> GetById(int id)
        {
            // Busca el paquete sin importar el estado
            var paquete = await _context.Paquetes
                .Include(p => p.DetallePaquetes)
                    .ThenInclude(d => d.Servicio)
                .FirstOrDefaultAsync(p => p.Id == id);

            // Solo falla si realmente NO existe en la BD
            if (paquete == null) return NotFound();
            return Ok(paquete);
        }

        [HttpPost]
        public async Task<ActionResult<Paquete>> Create([FromBody] PaqueteInput input)
        {
            if (input == null)
                return BadRequest("El objeto paquete es requerido");

            if (string.IsNullOrWhiteSpace(input.Nombre))
                return BadRequest("El nombre es requerido");

            var paquete = new Paquete
            {
                Nombre = input.Nombre,
                Descripcion = input.Descripcion,
                Precio = input.Precio,
                DuracionMinutos = input.DuracionMinutos,
                Estado = true
            };

            _context.Paquetes.Add(paquete);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = paquete.Id }, paquete);
        }

        [HttpPost("completo")]
        public async Task<ActionResult<Paquete>> CreateCompleto([FromBody] PaqueteConDetallesInput input)
        {
            if (input == null)
                return BadRequest("El objeto paquete es requerido");

            if (string.IsNullOrWhiteSpace(input.Nombre))
                return BadRequest("El nombre es requerido");

            if (input.Detalles == null || !input.Detalles.Any())
                return BadRequest("El paquete debe tener al menos un detalle");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var paquete = new Paquete
                {
                    Nombre = input.Nombre,
                    Descripcion = input.Descripcion,
                    Precio = input.Precio,
                    DuracionMinutos = input.DuracionMinutos,
                    Estado = true
                };

                foreach (var detInput in input.Detalles)
                {
                    if (detInput.ServicioId <= 0 || detInput.Cantidad <= 0)
                        return BadRequest("ServicioId y Cantidad son obligatorios y mayores a cero");
                    
                    var servicioExiste = await _context.Servicios.AnyAsync(s => s.Id == detInput.ServicioId);
                    if (!servicioExiste)
                        return BadRequest($"El servicio con id {detInput.ServicioId} no existe");

                    var detalle = new DetallePaquete
                    {
                        ServicioId = detInput.ServicioId,
                        Cantidad = detInput.Cantidad
                    };
                    paquete.DetallePaquetes.Add(detalle);
                }

                _context.Paquetes.Add(paquete);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Retornar paquete con detalles cargados
                var resultado = await _context.Paquetes
                    .Include(p => p.DetallePaquetes)
                        .ThenInclude(d => d.Servicio)
                    .FirstOrDefaultAsync(p => p.Id == paquete.Id);

                return CreatedAtAction(nameof(GetById), new { id = resultado.Id }, resultado);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Paquete paquete)
        {
            if (id != paquete.Id) return BadRequest();

            // Busca el paquete existente sin importar el estado
            var paqueteExistente = await _context.Paquetes.FindAsync(id);
            // Solo falla si realmente NO existe en la BD
            if (paqueteExistente == null) return NotFound();

            _context.Entry(paqueteExistente).CurrentValues.SetValues(paquete);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Paquetes.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        public class DetalleUpdateInput
        {
            public int ServicioId { get; set; }
            public int Cantidad { get; set; }
        }
        public class PaqueteDetallesUpdateInput
        {
            public List<DetalleUpdateInput> Detalles { get; set; } = new List<DetalleUpdateInput>();
        }

        [HttpPut("{id}/detalles")]
        public async Task<ActionResult<Paquete>> UpdateDetalles(int id, [FromBody] PaqueteDetallesUpdateInput input)
        {
            var paquete = await _context.Paquetes
                .Include(p => p.DetallePaquetes)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (paquete == null) return NotFound();

            if (input == null || input.Detalles == null || !input.Detalles.Any())
                return BadRequest("El paquete debe tener al menos un detalle");

            foreach (var det in input.Detalles)
            {
                if (det.ServicioId <= 0 || det.Cantidad <= 0)
                    return BadRequest("ServicioId y Cantidad son obligatorios y mayores a cero");
                var servicioExiste = await _context.Servicios.AnyAsync(s => s.Id == det.ServicioId);
                if (!servicioExiste)
                    return BadRequest($"El servicio con id {det.ServicioId} no existe");
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.DetallePaquetes.RemoveRange(paquete.DetallePaquetes);
                await _context.SaveChangesAsync();

                foreach (var det in input.Detalles)
                {
                    var nuevo = new DetallePaquete
                    {
                        PaqueteId = id,
                        ServicioId = det.ServicioId,
                        Cantidad = det.Cantidad
                    };
                    _context.DetallePaquetes.Add(nuevo);
                }
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                var resultado = await _context.Paquetes
                    .Include(p => p.DetallePaquetes)
                        .ThenInclude(d => d.Servicio)
                    .FirstOrDefaultAsync(p => p.Id == id);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpPut("{id}/estado")]
        public async Task<ActionResult<CambioEstadoResponse<Paquete>>> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        {
            var paquete = await _context.Paquetes
                .Include(p => p.DetallePaquetes)
                    .ThenInclude(d => d.Servicio)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paquete == null) return NotFound();

            // Actualizar solo el estado
            paquete.Estado = input.estado;
            await _context.SaveChangesAsync();

            var response = new CambioEstadoResponse<Paquete>
            {
                entidad = paquete,
                mensaje = input.estado ? "Paquete activado exitosamente" : "Paquete desactivado exitosamente",
                exitoso = true
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Busca el paquete sin importar el estado
            var paquete = await _context.Paquetes
                .Include(p => p.DetallePaquetes)
                .Include(p => p.Agendamientos)
                .FirstOrDefaultAsync(p => p.Id == id);

            // Solo falla si realmente NO existe en la BD
            if (paquete == null) return NotFound();

            // Verificar si tiene ventas o agendamientos activos
            bool tieneVentas = await _context.DetalleVentas.AnyAsync(d => d.PaqueteId == id);
            bool tieneAgendamientosActivos = paquete.Agendamientos.Any(a => a.Estado != "Cancelada");

            if (tieneVentas || tieneAgendamientosActivos)
            {
                // Soft Delete: Cambia el estado a false
                paquete.Estado = false;
                await _context.SaveChangesAsync();
                return Ok(new { 
                    message = "Paquete desactivado (borrado lógico por tener registros asociados)", 
                    eliminado = true, 
                    fisico = false,
                    motivo = tieneVentas ? "Ventas registradas" : "Agendamientos activos",
                    detallesAsociados = paquete.DetallePaquetes.Count()
                });
            }

            // Borrado Físico: No tiene registros críticos
            // Eliminar detalles primero
            _context.DetallePaquetes.RemoveRange(paquete.DetallePaquetes);
            _context.Paquetes.Remove(paquete);

            await _context.SaveChangesAsync();
            return Ok(new { 
                message = "Paquete eliminado físicamente de la base de datos", 
                eliminado = true, 
                fisico = true 
            });
        }
    }
}
    
