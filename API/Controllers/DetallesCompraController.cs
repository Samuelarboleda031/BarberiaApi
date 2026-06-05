using BarberiaApi.Application.Common;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class DetallesCompraController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public DetallesCompraController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet("compra/{compraId}")]
        public async Task<ActionResult<object>> GetByCompra(int compraId, [FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            PaginationHelper.Sanitize(ref page, ref pageSize);
            var baseQ = _context.DetalleCompras
                .Include(d => d.Producto)
                .Where(d => d.CompraId == compraId)
                .AsNoTracking()
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
