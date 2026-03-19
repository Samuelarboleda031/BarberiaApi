using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Application.DTOs;
using Microsoft.AspNetCore.Http;
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
    public class RolesModulosController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public RolesModulosController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.RolesModulos
                .Include(rm => rm.Rol)
                .Include(rm => rm.Modulo)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(rm =>
                    (rm.Rol != null && rm.Rol.Nombre != null && rm.Rol.Nombre.ToLower().Contains(term)) ||
                    (rm.Modulo != null && rm.Modulo.Nombre != null && rm.Modulo.Nombre.ToLower().Contains(term))
                );
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("role/{rolId}")]
        public async Task<ActionResult<object>> GetByRole(int rolId, [FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.RolesModulos
                .Include(rm => rm.Modulo)
                .Where(rm => rm.RolId == rolId)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(rm =>
                    rm.Modulo != null && rm.Modulo.Nombre != null && rm.Modulo.Nombre.ToLower().Contains(term)
                );
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
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
