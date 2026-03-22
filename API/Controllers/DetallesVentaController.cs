using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Application.DTOs;
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
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.DetalleVentas
                .Include(d => d.Producto)
                .Include(d => d.Servicio)
                .Include(d => d.Paquete)
                .AsNoTracking()
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = $"%{q.Trim()}%";
                baseQ = baseQ.Where(d =>
                    (d.Producto != null && d.Producto.Nombre != null && EF.Functions.Like(d.Producto.Nombre, term)) ||
                    (d.Servicio != null && d.Servicio.Nombre != null && EF.Functions.Like(d.Servicio.Nombre, term)) ||
                    (d.Paquete != null && d.Paquete.Nombre != null && EF.Functions.Like(d.Paquete.Nombre, term))
                );
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
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
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (detalle == null) return NotFound();
            return Ok(detalle);
        }

        [HttpGet("venta/{ventaId}")]
        public async Task<ActionResult<object>> GetByVenta(int ventaId, [FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.DetalleVentas
                .Include(d => d.Producto)
                .Include(d => d.Servicio)
                .Include(d => d.Paquete)
                .AsNoTracking()
                .Where(d => d.VentaId == ventaId)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = $"%{q.Trim()}%";
                baseQ = baseQ.Where(d =>
                    (d.Producto != null && d.Producto.Nombre != null && EF.Functions.Like(d.Producto.Nombre, term)) ||
                    (d.Servicio != null && d.Servicio.Nombre != null && EF.Functions.Like(d.Servicio.Nombre, term)) ||
                    (d.Paquete != null && d.Paquete.Nombre != null && EF.Functions.Like(d.Paquete.Nombre, term))
                );
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        // B5: Endpoint bulk para obtener detalles de múltiples ventas en una sola petición
        [HttpGet("por-ventas")]
        public async Task<ActionResult<object>> GetByVentas([FromQuery] string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return BadRequest("Se requiere el parámetro 'ids' con IDs de ventas separados por coma");

            var ventaIds = ids.Split(',')
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .Take(100) // Limitar a 100 ventas máximo
                .ToList();

            if (ventaIds.Count == 0)
                return BadRequest("No se proporcionaron IDs válidos");

            var detalles = await _context.DetalleVentas
                .Include(d => d.Producto)
                .Include(d => d.Servicio)
                .Include(d => d.Paquete)
                .AsNoTracking()
                .Where(d => ventaIds.Contains(d.VentaId))
                .ToListAsync();

            var agrupados = detalles.GroupBy(d => d.VentaId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return Ok(agrupados);
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
