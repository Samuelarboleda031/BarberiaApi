using BarberiaApi.Models;
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
    public class CategoriasController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public CategoriasController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            var q = _context.Categorias
                .OrderBy(c => c.Nombre)
                .AsQueryable();
            var totalCount = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Categoria>> GetById(int id)
        {
            // Busca la categoría sin importar el estado
            var categoria = await _context.Categorias
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            // Solo falla si realmente NO existe en la BD
            if (categoria == null) return NotFound();
            return Ok(categoria);
        }

        [HttpPost]
        public async Task<ActionResult<Categoria>> Create([FromBody] Categoria? categoria)
        {
            if (categoria == null)
                return BadRequest("El objeto categoria es requerido");

            if (string.IsNullOrWhiteSpace(categoria.Nombre))
                return BadRequest("El nombre de la categoría es requerido");

            if (await _context.Categorias.AnyAsync(c => c.Nombre == categoria.Nombre && c.Estado == true))
                return BadRequest("Ya existe una categoría con ese nombre");

            categoria.Id = 0;
            categoria.Estado = true;

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Categoria categoria)
        {
            if (id != categoria.Id) return BadRequest();

            // Busca la categoría existente sin importar el estado
            var categoriaExistente = await _context.Categorias.FindAsync(id);
            // Solo falla si realmente NO existe en la BD
            if (categoriaExistente == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(categoria.Nombre) && categoria.Nombre != categoriaExistente.Nombre)
            {
                if (await _context.Categorias.AnyAsync(c => c.Nombre == categoria.Nombre && c.Id != id && c.Estado == true))
                    return BadRequest("Ya existe otra categoría con ese nombre");
            }

            _context.Entry(categoriaExistente).CurrentValues.SetValues(categoria);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Categorias.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpPut("{id}/estado")]
        public async Task<ActionResult<CambioEstadoResponse<Categoria>>> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        {
            var categoria = await _context.Categorias
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria == null) return NotFound();

            // Actualizar solo el estado
            categoria.Estado = input.estado;
            await _context.SaveChangesAsync();

            var response = new CambioEstadoResponse<Categoria>
            {
                entidad = categoria,
                mensaje = input.estado ? "Categoría activada exitosamente" : "Categoría desactivada exitosamente",
                exitoso = true
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Busca la categoría sin importar el estado
            var categoria = await _context.Categorias
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            // Solo falla si realmente NO existe en la BD
            if (categoria == null) return NotFound();

            // Verificar si tiene productos activos
            bool tieneProductosActivos = categoria.Productos.Any(p => p.Estado == true);

            if (tieneProductosActivos)
            {
                // Soft Delete: Cambia el estado a false
                categoria.Estado = false;
                await _context.SaveChangesAsync();
                return Ok(new { 
                    message = "Categoría desactivada (borrado lógico por tener productos activos asociados)", 
                    eliminado = true, 
                    fisico = false,
                    productosAsociados = categoria.Productos.Count(p => p.Estado == true)
                });
            }

            // Borrado Físico: No tiene productos activos
            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();
            return Ok(new { 
                message = "Categoría eliminada físicamente de la base de datos", 
                eliminado = true, 
                fisico = true 
            });
        }
    }
}
