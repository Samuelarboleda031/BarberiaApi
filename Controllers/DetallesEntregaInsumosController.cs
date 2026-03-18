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
    public class DetallesEntregaInsumosController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public DetallesEntregaInsumosController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.DetalleEntregasInsumos
                .Include(d => d.Entrega)
                    .ThenInclude(e => e.Barbero)
                .Include(d => d.Producto)
                    .ThenInclude(p => p.Categoria)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(d =>
                    (d.Producto != null && d.Producto.Nombre != null && d.Producto.Nombre.ToLower().Contains(term)) ||
                    (d.Entrega != null && d.Entrega.Estado != null && d.Entrega.Estado.ToLower().Contains(term))
                );
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DetalleEntregasInsumo>> GetById(int id)
        {
            var detalle = await _context.DetalleEntregasInsumos
                .Include(d => d.Entrega)
                    .ThenInclude(e => e.Barbero)
                .Include(d => d.Producto)
                    .ThenInclude(p => p.Categoria)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (detalle == null) return NotFound();
            return Ok(detalle);
        }

        [HttpGet("entrega/{entregaId}")]
        public async Task<ActionResult<object>> GetByEntregaId(int entregaId, [FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.DetalleEntregasInsumos
                .Include(d => d.Entrega)
                    .ThenInclude(e => e.Barbero)
                .Include(d => d.Producto)
                    .ThenInclude(p => p.Categoria)
                .Where(d => d.EntregaId == entregaId)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(d => d.Producto != null && d.Producto.Nombre != null && d.Producto.Nombre.ToLower().Contains(term));
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("producto/{productoId}")]
        public async Task<ActionResult<object>> GetByProductoId(int productoId, [FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.DetalleEntregasInsumos
                .Include(d => d.Entrega)
                    .ThenInclude(e => e.Barbero)
                .Include(d => d.Producto)
                    .ThenInclude(p => p.Categoria)
                .Where(d => d.ProductoId == productoId)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(d => d.Producto != null && d.Producto.Nombre != null && d.Producto.Nombre.ToLower().Contains(term));
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }
    }
}
