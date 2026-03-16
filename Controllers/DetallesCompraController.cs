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
    public class DetallesCompraController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public DetallesCompraController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet("compra/{compraId}")]
        public async Task<ActionResult<object>> GetByCompra(int compraId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            var q = _context.DetalleCompras
                .Include(d => d.Producto)
                .Where(d => d.CompraId == compraId)
                .AsQueryable();
            var totalCount = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }
    }
}
