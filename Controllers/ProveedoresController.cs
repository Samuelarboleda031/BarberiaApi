using BarberiaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProveedoresController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public ProveedoresController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.Proveedores.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(p =>
                    (p.Nombre != null && p.Nombre.ToLower().Contains(term)) ||
                    (p.NIT != null && p.NIT.ToLower().Contains(term)) ||
                    (p.Contacto != null && p.Contacto.ToLower().Contains(term)) ||
                    (p.Correo != null && p.Correo.ToLower().Contains(term)) ||
                    (p.Telefono != null && p.Telefono.ToLower().Contains(term))
                );
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ.OrderBy(p => p.Nombre).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("naturales")]
        public async Task<ActionResult<object>> GetNaturales([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.Proveedores
                .Where(p => p.TipoProveedor == "Natural")
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(p =>
                    (p.Nombre != null && p.Nombre.ToLower().Contains(term)) ||
                    (p.NIT != null && p.NIT.ToLower().Contains(term)) ||
                    (p.Contacto != null && p.Contacto.ToLower().Contains(term)) ||
                    (p.Correo != null && p.Correo.ToLower().Contains(term)) ||
                    (p.Telefono != null && p.Telefono.ToLower().Contains(term))
                );
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ.OrderBy(p => p.Nombre).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("juridicos")]
        public async Task<ActionResult<object>> GetJuridicos([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.Proveedores
                .Where(p => p.TipoProveedor == "Juridico")
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(p =>
                    (p.Nombre != null && p.Nombre.ToLower().Contains(term)) ||
                    (p.NIT != null && p.NIT.ToLower().Contains(term)) ||
                    (p.Contacto != null && p.Contacto.ToLower().Contains(term)) ||
                    (p.Correo != null && p.Correo.ToLower().Contains(term)) ||
                    (p.Telefono != null && p.Telefono.ToLower().Contains(term))
                );
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ.OrderBy(p => p.Nombre).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Proveedor>> GetById(int id)
        {
            // Busca el proveedor sin importar el estado
            var proveedor = await _context.Proveedores
                .Include(p => p.Compras)
                .FirstOrDefaultAsync(p => p.Id == id);

            // Solo falla si realmente NO existe en la BD
            if (proveedor == null) return NotFound();
            return Ok(proveedor);
        }

        [HttpPost("natural")]
        public async Task<ActionResult<Proveedor>> CreateNatural([FromBody] ProveedorNaturalInput input)
        {
            if (input == null)
                return BadRequest("El objeto proveedor natural es requerido");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (await _context.Proveedores.AnyAsync(p => p.NIT == input.NIT))
                return BadRequest("Ya existe un proveedor con ese NIT");

            var proveedor = new Proveedor
            {
                Nombre = input.Nombre,
                Contacto = input.Contacto,
                NumeroIdentificacion = input.NumeroIdentificacion,
                TipoIdentificacion = input.TipoIdentificacion,
                Correo = input.Correo,
                Telefono = input.Telefono,
                Direccion = input.Direccion,
                NIT = input.NIT,
                CorreoContacto = input.CorreoContacto,
                TelefonoContacto = input.TelefonoContacto,
                TipoProveedor = "Natural",
                Estado = true
            };

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = proveedor.Id }, proveedor);
        }

        [HttpPost("juridico")]
        public async Task<ActionResult<Proveedor>> CreateJuridico([FromBody] ProveedorJuridicoInput input)
        {
            if (input == null)
                return BadRequest("El objeto proveedor jurídico es requerido");

            // Validar NIT unico
            if (await _context.Proveedores.AnyAsync(p => p.NIT == input.NIT))
                return BadRequest("Ya existe un proveedor con ese NIT");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var proveedor = new Proveedor
            {
                Nombre = input.Nombre,
                NIT = input.NIT,
                Correo = input.Correo,
                Telefono = input.Telefono,
                Direccion = input.Direccion,
                Contacto = input.Contacto,
                NumeroIdentificacion = input.NumeroIdentificacion,
                TipoIdentificacion = input.TipoIdentificacion,
                CorreoContacto = input.CorreoContacto,
                TelefonoContacto = input.TelefonoContacto,
                TipoProveedor = "Juridico",
                Estado = true
            };

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = proveedor.Id }, proveedor);
        }

        [HttpPost]
        public async Task<ActionResult<Proveedor>> Create([FromBody] ProveedorCreateInput input)
        {
            if (input == null)
                return BadRequest("El objeto proveedor es requerido");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var tipo = (input.TipoProveedor ?? string.Empty).Trim();
            if (tipo != "Natural" && tipo != "Juridico")
                return BadRequest("TipoProveedor debe ser 'Natural' o 'Juridico'");
            if (await _context.Proveedores.AnyAsync(p => p.NIT == input.NIT))
                return BadRequest("Ya existe un proveedor con ese NIT");

            var proveedor = new Proveedor
            {
                Nombre = input.Nombre,
                NIT = input.NIT,
                Correo = input.Correo,
                Telefono = input.Telefono,
                Direccion = input.Direccion,
                Contacto = input.Contacto,
                NumeroIdentificacion = input.NumeroIdentificacion,
                TipoIdentificacion = string.IsNullOrWhiteSpace(input.TipoIdentificacion)
                    ? (tipo == "Natural" ? "CC" : "NIT")
                    : input.TipoIdentificacion,
                CorreoContacto = input.CorreoContacto,
                TelefonoContacto = input.TelefonoContacto,
                TipoProveedor = tipo,
                Estado = true
            };

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = proveedor.Id }, proveedor);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProveedorUpdateInput input)
        {
            var proveedorExistente = await _context.Proveedores.FindAsync(id);
            if (proveedorExistente == null) return NotFound();

            // Actualizar campos comunes
            proveedorExistente.Nombre = input.Nombre;
            if (!string.IsNullOrWhiteSpace(input.NIT))
            {
                var nitDuplicado = await _context.Proveedores.AnyAsync(p => p.NIT == input.NIT && p.Id != id);
                if (nitDuplicado) return BadRequest("Ya existe un proveedor con ese NIT");
                proveedorExistente.NIT = input.NIT;
            }
            proveedorExistente.Correo = input.Correo;
            proveedorExistente.Telefono = input.Telefono;
            proveedorExistente.Direccion = input.Direccion;
            if (input.Estado.HasValue)
                proveedorExistente.Estado = input.Estado.Value;

            // Actualizar campos según tipo de proveedor
            if (proveedorExistente.TipoProveedor == "Natural")
            {
                proveedorExistente.Contacto = input.Contacto;
                proveedorExistente.NumeroIdentificacion = input.NumeroIdentificacion;
                proveedorExistente.TipoIdentificacion = input.TipoIdentificacion;
                proveedorExistente.CorreoContacto = input.CorreoContacto;
                proveedorExistente.TelefonoContacto = input.TelefonoContacto;
            }
            else if (proveedorExistente.TipoProveedor == "Juridico")
            {
                proveedorExistente.Contacto = input.Contacto;
                proveedorExistente.NumeroIdentificacion = input.NumeroIdentificacion;
                proveedorExistente.TipoIdentificacion = input.TipoIdentificacion;
                proveedorExistente.CorreoContacto = input.CorreoContacto;
                proveedorExistente.TelefonoContacto = input.TelefonoContacto;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Proveedores.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult<CambioEstadoResponse<Proveedor>>> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        {
            var proveedor = await _context.Proveedores
                .Include(p => p.Compras)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proveedor == null) return NotFound();

            // Actualizar solo el estado
            proveedor.Estado = input.estado;
            await _context.SaveChangesAsync();

            var response = new CambioEstadoResponse<Proveedor>
            {
                entidad = proveedor,
                mensaje = input.estado ? "Proveedor activado exitosamente" : "Proveedor desactivado exitosamente",
                exitoso = true
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Busca el proveedor sin importar el estado
            var proveedor = await _context.Proveedores
                .Include(p => p.Compras)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            // Solo falla si realmente NO existe en la BD
            if (proveedor == null) return NotFound();

            // Verificar si tiene compras registradas
            bool tieneCompras = proveedor.Compras.Any();

            if (tieneCompras)
            {
                // Soft Delete: Cambia el estado a false
                proveedor.Estado = false;
                await _context.SaveChangesAsync();
                return Ok(new { 
                    message = "Proveedor desactivado (borrado lógico por tener compras asociadas)", 
                    eliminado = true, 
                    fisico = false,
                    comprasAsociadas = proveedor.Compras.Count()
                });
            }

            // Borrado Físico: No tiene compras registradas
            _context.Proveedores.Remove(proveedor);
            await _context.SaveChangesAsync();
            return Ok(new { 
                message = "Proveedor eliminado físicamente de la base de datos", 
                eliminado = true, 
                fisico = true 
            });
        }
    }
}
