using BarberiaApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiciosController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public ServiciosController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Servicio>>> GetAll()
        {
            return await _context.Servicios
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Servicio>> GetById(int id)
        {
            // Busca el servicio sin importar el estado
            var servicio = await _context.Servicios
                .Include(s => s.DetallePaquetes)
                .FirstOrDefaultAsync(s => s.Id == id);

            // Solo falla si realmente NO existe en la BD
            if (servicio == null) return NotFound();
            return Ok(servicio);
        }

        [HttpPost]
        public async Task<ActionResult<Servicio>> Create([FromBody] Servicio? servicio)
        {
            if (servicio == null)
                return BadRequest("El objeto servicio es requerido");

            if (string.IsNullOrWhiteSpace(servicio.Nombre))
                return BadRequest("El nombre del servicio es requerido");

            if (servicio.Precio <= 0)
                return BadRequest("El precio debe ser mayor a cero");

            servicio.Id = 0;
            servicio.Estado = true;

            _context.Servicios.Add(servicio);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = servicio.Id }, servicio);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Servicio servicio)
        {
            if (id != servicio.Id) return BadRequest();

            // Busca el servicio existente sin importar el estado
            var servicioExistente = await _context.Servicios.FindAsync(id);
            // Solo falla si realmente NO existe en la BD
            if (servicioExistente == null) return NotFound();

            _context.Entry(servicioExistente).CurrentValues.SetValues(servicio);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Servicios.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult<CambioEstadoResponse<Servicio>>> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        {
            var servicio = await _context.Servicios
                .Include(s => s.DetallePaquetes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (servicio == null) return NotFound();

            // Actualizar solo el estado
            servicio.Estado = input.estado;
            await _context.SaveChangesAsync();

            var response = new CambioEstadoResponse<Servicio>
            {
                entidad = servicio,
                mensaje = input.estado ? "Servicio activado exitosamente" : "Servicio desactivado exitosamente",
                exitoso = true
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Busca el servicio sin importar el estado
            var servicio = await _context.Servicios
                .Include(s => s.Agendamientos)
                .Include(s => s.DetalleVenta)
                .Include(s => s.DetallePaquetes)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            // Solo falla si realmente NO existe en la BD
            if (servicio == null) return NotFound();

            // Verificar si tiene agendamientos activos, ventas o está en paquetes
            bool tieneAgendamientosActivos = servicio.Agendamientos.Any(a => a.Estado != "Cancelada");
            bool tieneVentas = servicio.DetalleVenta.Any();
            bool estaEnPaquetes = servicio.DetallePaquetes.Any();

            if (tieneAgendamientosActivos || tieneVentas || estaEnPaquetes)
            {
                // Soft Delete: Cambia el estado a false
                servicio.Estado = false;
                await _context.SaveChangesAsync();
                return Ok(new { 
                    message = "Servicio desactivado (borrado lógico por tener registros asociados)", 
                    eliminado = true, 
                    fisico = false,
                    motivo = tieneAgendamientosActivos ? "Agendamientos activos" : 
                            tieneVentas ? "Ventas registradas" : "Incluido en paquetes"
                });
            }

            // Borrado Físico: No tiene registros críticos
            _context.Servicios.Remove(servicio);
            await _context.SaveChangesAsync();
            return Ok(new { 
                message = "Servicio eliminado físicamente de la base de datos", 
                eliminado = true, 
                fisico = true 
            });
        }
    }
}
