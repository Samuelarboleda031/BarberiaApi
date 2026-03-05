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
    public class DetallesCompraController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public DetallesCompraController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet("compra/{compraId}")]
        public async Task<ActionResult<IEnumerable<DetalleCompra>>> GetByCompra(int compraId)
        {
            var detalles = await _context.DetalleCompras
                .Include(d => d.Producto)
                .Where(d => d.CompraId == compraId)
                .ToListAsync();

            return Ok(detalles);
        }
    }
}
