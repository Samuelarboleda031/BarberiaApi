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
    public class DetallesEntregaInsumosController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public DetallesEntregaInsumosController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DetalleEntregasInsumo>>> GetAll()
        {
            var detalles = await _context.DetalleEntregasInsumos
                .Include(d => d.Entrega)
                    .ThenInclude(e => e.Barbero)
                .Include(d => d.Producto)
                    .ThenInclude(p => p.Categoria)
                .ToListAsync();

            return Ok(detalles);
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
        public async Task<ActionResult<IEnumerable<DetalleEntregasInsumo>>> GetByEntregaId(int entregaId)
        {
            var detalles = await _context.DetalleEntregasInsumos
                .Include(d => d.Entrega)
                    .ThenInclude(e => e.Barbero)
                .Include(d => d.Producto)
                    .ThenInclude(p => p.Categoria)
                .Where(d => d.EntregaId == entregaId)
                .ToListAsync();

            return Ok(detalles);
        }

        [HttpGet("producto/{productoId}")]
        public async Task<ActionResult<IEnumerable<DetalleEntregasInsumo>>> GetByProductoId(int productoId)
        {
            var detalles = await _context.DetalleEntregasInsumos
                .Include(d => d.Entrega)
                    .ThenInclude(e => e.Barbero)
                .Include(d => d.Producto)
                    .ThenInclude(p => p.Categoria)
                .Where(d => d.ProductoId == productoId)
                .ToListAsync();

            return Ok(detalles);
        }
    }
}
