using BarberiaApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.OutputCaching;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public RolesController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.Roles
                .AsNoTracking()
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(r =>
                    (r.Nombre != null && r.Nombre.ToLower().Contains(term)) ||
                    (r.Descripcion != null && r.Descripcion.ToLower().Contains(term))
                );
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ
                .OrderBy(r => r.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    Estado = r.Estado ?? false,
                    UsuariosAsignados = r.Usuarios.Count,
                    Modulos = r.RolesModulos.Select(rm => rm.ModuloId).ToList()
                })
                .ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Role>> GetById(int id)
        {
            // Busca el rol sin importar el estado
            var rol = await _context.Roles
                .Include(r => r.RolesModulos)
                    .ThenInclude(rm => rm.Modulo)
                .Include(r => r.Usuarios)
                .FirstOrDefaultAsync(r => r.Id == id);

            // Solo falla si realmente NO existe en la BD
            if (rol == null) return NotFound();
            return Ok(rol);
        }

        [HttpPost]
        public async Task<ActionResult<Role>> Create([FromBody] Role? rol)
        {
            if (rol == null) return BadRequest();
            
            rol.Id = 0;
            rol.Estado = true;
            _context.Roles.Add(rol);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = rol.Id }, rol);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RoleInput input)
        {
            if (id != input.Id) return BadRequest();
            
            var rol = await _context.Roles.FindAsync(id);
            if (rol == null) return NotFound();

            rol.Nombre = input.Nombre;
            rol.Descripcion = input.Descripcion;
            rol.Estado = input.Estado;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Roles.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult<CambioEstadoResponse<Role>>> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        {
            var rol = await _context.Roles
                .Include(r => r.Usuarios)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rol == null) return NotFound();

            // Si se intenta desactivar, verificar que no tenga usuarios asociados
            if (!input.estado && rol.Usuarios.Any())
            {
                return Conflict(new
                {
                    message = "No se puede desactivar el rol porque tiene usuarios asociados",
                    exitoso = false,
                    usuariosAsociados = rol.Usuarios.Count()
                });
            }

            // Actualizar solo el estado
            rol.Estado = input.estado;
            await _context.SaveChangesAsync();

            var response = new CambioEstadoResponse<Role>
            {
                entidad = rol,
                mensaje = input.estado ? "Rol activado exitosamente" : "Rol desactivado exitosamente",
                exitoso = true
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var rol = await _context.Roles
                .Include(r => r.Usuarios)
                .Include(r => r.RolesModulos)
                .FirstOrDefaultAsync(r => r.Id == id);
            
            if (rol == null) return NotFound();

            bool tieneUsuariosAsociados = rol.Usuarios.Any();

            if (tieneUsuariosAsociados)
                return Conflict(new
                {
                    message = "No se puede eliminar el rol porque tiene usuarios asociados",
                    eliminado = false,
                    usuariosAsociados = rol.Usuarios.Count()
                });

            _context.Roles.Remove(rol);
            await _context.SaveChangesAsync();
            return Ok(new { 
                message = "Rol eliminado físicamente de la base de datos", 
                eliminado = true, 
                fisico = true,
                modulosEliminados = true
            });
        }
    }
}
