using BarberiaApi.Models;
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
    public class DetallesVentaController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public DetallesVentaController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            var q = _context.DetalleVentas
                .Include(d => d.Producto)
                .Include(d => d.Servicio)
                .Include(d => d.Paquete)
                .AsQueryable();
            var totalCount = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DetalleVenta>> GetById(int id)
        {
            var detalle = await _context.DetalleVentas
                .Include(d => d.Producto)
                .Include(d => d.Servicio)
                .Include(d => d.Paquete)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (detalle == null) return NotFound();
            return Ok(detalle);
        }

        [HttpGet("venta/{ventaId}")]
        public async Task<ActionResult<object>> GetByVenta(int ventaId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            var q = _context.DetalleVentas
                .Include(d => d.Producto)
                .Include(d => d.Servicio)
                .Include(d => d.Paquete)
                .Where(d => d.VentaId == ventaId)
                .AsQueryable();
            var totalCount = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpPost]
        public async Task<ActionResult<DetalleVenta>> Create([FromBody] DetalleVenta detalle)
        {
            if (detalle == null)
                return BadRequest("El detalle es requerido");

            // Validar que tenga al menos un elemento (producto, servicio o paquete)
            if (!detalle.ProductoId.HasValue && !detalle.ServicioId.HasValue && !detalle.PaqueteId.HasValue)
                return BadRequest("El detalle debe tener al menos un producto, servicio o paquete");

            _context.DetalleVentas.Add(detalle);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = detalle.Id }, detalle);
        }
    }
}
