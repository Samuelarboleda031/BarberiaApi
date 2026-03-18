    using BarberiaApi.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System;
    using RoleModel = BarberiaApi.Models.Role;

    namespace BarberiaApi.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        public class ModulosController : ControllerBase
        {
            private readonly BarberiaContext _context;

            public ModulosController(BarberiaContext context)
            {
                _context = context;
            }

            [HttpGet]
            public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 5;
                var baseQ = _context.Modulos.AsQueryable();
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var term = q.Trim().ToLower();
                    baseQ = baseQ.Where(m => m.Nombre != null && m.Nombre.ToLower().Contains(term));
                }
                var totalCount = await baseQ.CountAsync();
                var items = await baseQ.OrderBy(m => m.Nombre).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                return Ok(new { items, totalCount, page, pageSize, totalPages });
            }

            [HttpGet("{id}")]
            public async Task<ActionResult<Modulos>> GetById(int id)
            {
                // Busca el módulo sin importar el estado
                var modulo = await _context.Modulos.FindAsync(id);
                // Solo falla si realmente NO existe en la BD
                if (modulo == null) return NotFound();
                return Ok(modulo);
            }

            [HttpPost]
            public async Task<ActionResult<Modulos>> Create([FromBody] Modulos modulo)
            {
                modulo.Id = 0;
                modulo.Estado = true;
                _context.Modulos.Add(modulo);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetById), new { id = modulo.Id }, modulo);
            }

            [HttpPut("{id}")]
            public async Task<IActionResult> Update(int id, [FromBody] Modulos modulo)
            {
                if (id != modulo.Id) return BadRequest();
                _context.Entry(modulo).State = EntityState.Modified;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Modulos.AnyAsync(m => m.Id == id))
                        return NotFound();
                    throw;
                }
                return NoContent();
            }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var modulo = await _context.Modulos
            .Include(m => m.RolesModulos)
                .ThenInclude(rm => rm.Rol)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (modulo == null) return NotFound();

        bool tieneRolesActivos = modulo.RolesModulos
            .Any(rm => rm.Rol != null && rm.Rol.Estado == true);

        if (tieneRolesActivos)
        {
            modulo.Estado = false;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Módulo desactivado (borrado lógico por estar asignado a roles activos)",
                eliminado = true,
                fisico = false,
                rolesAsociados = modulo.RolesModulos
                    .Count(rm => rm.Rol != null && rm.Rol.Estado == true)
            });
        }

        _context.Modulos.Remove(modulo);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Módulo eliminado físicamente de la base de datos",
            eliminado = true,
            fisico = true
        });
    }
}
}
