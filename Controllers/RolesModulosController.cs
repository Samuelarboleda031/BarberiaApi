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
    public class RolesModulosController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public RolesModulosController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RolesModulos>>> GetAll()
        {
            return await _context.RolesModulos
                .Include(rm => rm.Rol)
                .Include(rm => rm.Modulo)
                .ToListAsync();
        }

        [HttpGet("role/{rolId}")]
        public async Task<ActionResult<IEnumerable<RolesModulos>>> GetByRole(int rolId)
        {
            return await _context.RolesModulos
                .Include(rm => rm.Modulo)
                .Where(rm => rm.RolId == rolId)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<RolesModulos>> Create([FromBody] RolesModulos rm)
        {
            rm.Id = 0;
            _context.RolesModulos.Add(rm);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = rm.Id }, rm);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RolesModulos rm)
        {
            if (id != rm.Id) return BadRequest();
            _context.Entry(rm).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.RolesModulos.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.RolesModulos.FindAsync(id);
            if (item == null) return NotFound();
            _context.RolesModulos.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
