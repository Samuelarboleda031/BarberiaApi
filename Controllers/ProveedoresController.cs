using BarberiaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<ActionResult<IEnumerable<Proveedor>>> GetAll()
        {
            return await _context.Proveedores
                .OrderBy(p => p.Nombre)
                .ToListAsync();
        }

        [HttpGet("naturales")]
        public async Task<ActionResult<IEnumerable<Proveedor>>> GetNaturales()
        {
            return await _context.Proveedores
                .Where(p => p.TipoProveedor == "Natural")
                .OrderBy(p => p.Nombre)
                .ToListAsync();
        }

        [HttpGet("juridicos")]
        public async Task<ActionResult<IEnumerable<Proveedor>>> GetJuridicos()
        {
            return await _context.Proveedores
                .Where(p => p.TipoProveedor == "Juridico")
                .OrderBy(p => p.Nombre)
                .ToListAsync();
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

            if (string.IsNullOrWhiteSpace(input.Nombre))
                return BadRequest("El nombre es requerido");


            // Validar NIT unico si se proporciona
            if (!string.IsNullOrWhiteSpace(input.NIT))
            {
                if (await _context.Proveedores.AnyAsync(p => p.NIT == input.NIT))
                    return BadRequest("Ya existe un proveedor con ese NIT");
            }

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

            if (string.IsNullOrWhiteSpace(input.Nombre))
                return BadRequest("El nombre de la empresa es requerido");

            if (string.IsNullOrWhiteSpace(input.RazonSocial))
                return BadRequest("La razón social es requerida para persona jurídica");

            if (string.IsNullOrWhiteSpace(input.NIT))
                return BadRequest("El NIT es requerido para persona jurídica");

            // Validar NIT unico
            if (await _context.Proveedores.AnyAsync(p => p.NIT == input.NIT))
                return BadRequest("Ya existe un proveedor con ese NIT");

            var proveedor = new Proveedor
            {
                Nombre = input.Nombre,
                RazonSocial = input.RazonSocial,
                NIT = input.NIT,
                RepresentanteLegal = input.RepresentanteLegal,
                NumeroIdentificacionRepLegal = input.NumeroIdentificacionRepLegal,
                CargoRepLegal = input.CargoRepLegal,
                Correo = input.Correo,
                Telefono = input.Telefono,
                Direccion = input.Direccion,
                Ciudad = input.Ciudad,
                Departamento = input.Departamento,
                TipoProveedor = "Juridico",
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
            }
            else if (proveedorExistente.TipoProveedor == "Juridico")
            {
                proveedorExistente.RazonSocial = input.RazonSocial;
                proveedorExistente.RepresentanteLegal = input.RepresentanteLegal;
                proveedorExistente.NumeroIdentificacionRepLegal = input.NumeroIdentificacionRepLegal;
                proveedorExistente.CargoRepLegal = input.CargoRepLegal;
                proveedorExistente.Ciudad = input.Ciudad;
                proveedorExistente.Departamento = input.Departamento;
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
