using BarberiaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DetallePaquetesController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public DetallePaquetesController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DetallePaquete>>> GetAll()
        {
            var detalles = await _context.DetallePaquetes
                .Include(dp => dp.Paquete)
                .Include(dp => dp.Servicio)
                .ToListAsync();

            return Ok(detalles);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DetallePaquete>> GetById(int id)
        {
            var detalle = await _context.DetallePaquetes
                .Include(dp => dp.Paquete)
                .Include(dp => dp.Servicio)
                .FirstOrDefaultAsync(dp => dp.Id == id);

            if (detalle == null) return NotFound();
            return Ok(detalle);
        }

        [HttpGet("paquete/{paqueteId}")]
        public async Task<ActionResult<IEnumerable<DetallePaquete>>> GetByPaquete(int paqueteId)
        {
            var detalles = await _context.DetallePaquetes
                .Include(dp => dp.Servicio)
                .Where(dp => dp.PaqueteId == paqueteId)
                .ToListAsync();

            return Ok(detalles);
        }

        [HttpPost]
        public async Task<ActionResult<DetallePaquete>> Create([FromBody] DetallePaquete detalle)
        {
            if (detalle == null)
                return BadRequest("El detalle es requerido");

            if (detalle.PaqueteId <= 0 || detalle.ServicioId <= 0)
                return BadRequest("PaqueteId y ServicioId son obligatorios");

            if (detalle.Cantidad <= 0)
                return BadRequest("La cantidad debe ser mayor a cero");

            var paqueteExiste = await _context.Paquetes.AnyAsync(p => p.Id == detalle.PaqueteId);
            if (!paqueteExiste)
                return BadRequest("El paquete no existe");

            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == detalle.ServicioId);
            if (servicio == null)
                return BadRequest("El servicio no existe");


            // Ignorar propiedades de navegación
            detalle.Paquete = null!;
            detalle.Servicio = null!;

            _context.DetallePaquetes.Add(detalle);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = detalle.Id }, detalle);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DetallePaquete input)
        {
            if (input == null) return BadRequest("El detalle es requerido");
            var existing = await _context.DetallePaquetes.FirstOrDefaultAsync(dp => dp.Id == id);
            if (existing == null) return NotFound();
            existing.PaqueteId = input.PaqueteId;
            existing.ServicioId = input.ServicioId;
            existing.Cantidad = input.Cantidad;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var detalle = await _context.DetallePaquetes.FirstOrDefaultAsync(dp => dp.Id == id);
            if (detalle == null) return NotFound();
            _context.DetallePaquetes.Remove(detalle);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
