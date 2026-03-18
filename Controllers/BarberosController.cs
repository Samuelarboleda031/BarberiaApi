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
    public class BarberosController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public BarberosController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.Barberos
                .Include(b => b.Usuario)
                    .ThenInclude(u => u.Rol)
                .AsNoTracking()
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(b =>
                    (b.Usuario != null && (
                        (b.Usuario.Nombre != null && b.Usuario.Nombre.ToLower().Contains(term)) ||
                        (b.Usuario.Apellido != null && b.Usuario.Apellido.ToLower().Contains(term)) ||
                        (b.Usuario.Documento != null && b.Usuario.Documento.ToLower().Contains(term)) ||
                        (b.Usuario.Correo != null && b.Usuario.Correo.ToLower().Contains(term))
                    )) ||
                    (b.Telefono != null && b.Telefono.ToLower().Contains(term)) ||
                    (b.Direccion != null && b.Direccion.ToLower().Contains(term)) ||
                    (b.Barrio != null && b.Barrio.ToLower().Contains(term)) ||
                    (b.Especialidad != null && b.Especialidad.ToLower().Contains(term))
                );
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ
                .OrderBy(b => b.Usuario.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BarberoDto
                {
                    Id = b.Id,
                    UsuarioId = b.UsuarioId,
                    Nombre = b.Usuario.Nombre,
                    Apellido = b.Usuario.Apellido,
                    Documento = b.Usuario.Documento,
                    Correo = b.Usuario.Correo,
                    Telefono = b.Telefono,
                    Direccion = b.Direccion,
                    Barrio = b.Barrio,
                    FechaNacimiento = b.FechaNacimiento,
                    Especialidad = b.Especialidad,
                    FotoPerfil = b.Usuario.FotoPerfil,
                    Estado = b.Estado,
                    FechaContratacion = b.FechaContratacion,
                    Usuario = new UsuarioDto
                    {
                        Id = b.Usuario.Id,
                        Nombre = b.Usuario.Nombre,
                        Apellido = b.Usuario.Apellido,
                        Correo = b.Usuario.Correo,
                        RolId = b.Usuario.RolId,
                        RolNombre = b.Usuario.Rol != null ? b.Usuario.Rol.Nombre : null,
                        Estado = b.Usuario.Estado,
                        FechaCreacion = b.Usuario.FechaCreacion
                    }
                })
                .ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("{id}")]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<BarberoDto>> GetById(int id)
        {
            var barbero = await _context.Barberos
                .AsNoTracking()
                .Include(b => b.Usuario)
                    .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (barbero == null) 
                return NotFound();

            var barberoDto = new BarberoDto
            {
                Id = barbero.Id,
                UsuarioId = barbero.UsuarioId,
                Nombre = barbero.Usuario.Nombre,
                Apellido = barbero.Usuario.Apellido,
                Documento = barbero.Usuario.Documento,
                Correo = barbero.Usuario.Correo,
                Telefono = barbero.Telefono,
                Direccion = barbero.Direccion,
                Barrio = barbero.Barrio,
                FechaNacimiento = barbero.FechaNacimiento,
                Especialidad = barbero.Especialidad,
                FotoPerfil = barbero.Usuario.FotoPerfil,
                Estado = barbero.Estado,
                FechaContratacion = barbero.FechaContratacion,
                Usuario = new UsuarioDto
                {
                    Id = barbero.Usuario.Id,
                    Nombre = barbero.Usuario.Nombre,
                    Apellido = barbero.Usuario.Apellido,
                    Correo = barbero.Usuario.Correo,
                    RolId = barbero.Usuario.RolId,
                    RolNombre = barbero.Usuario.Rol?.Nombre,
                    Estado = barbero.Usuario.Estado,
                    FechaCreacion = barbero.Usuario.FechaCreacion
                }
            };

            return Ok(barberoDto);
        }

        [HttpPost]
        public async Task<ActionResult<BarberoDto>> Create([FromBody] BarberoInput input)
        {
            if (input == null)
                return BadRequest("El objeto barbero es requerido");

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(input.Nombre))
                return BadRequest("El nombre es requerido");

            if (string.IsNullOrWhiteSpace(input.Apellido))
                return BadRequest("El apellido es requerido");

            if (string.IsNullOrWhiteSpace(input.Documento))
                return BadRequest("El documento es requerido");

            if (string.IsNullOrWhiteSpace(input.Correo))
                return BadRequest("El correo es requerido");

            // 1. Validar que el usuario exista
            var usuario = await _context.Usuarios.FindAsync(input.UsuarioId);
            if (usuario == null)
                return BadRequest("El usuario no existe");

            // 2. Validar que el usuario tenga el rol correcto (Barbero)
            if (usuario.RolId != 2)
                return BadRequest("El usuario no tiene un rol de Barbero");

            // 3. Validar que no exista ya un perfil de barbero para este usuario
            if (await _context.Barberos.AnyAsync(b => b.UsuarioId == input.UsuarioId))
                return BadRequest("Ya existe un perfil de barbero para este usuario");

            // 4. Validar que el documento no exista en otros usuarios
            if (await _context.Usuarios.AnyAsync(u => u.Documento == input.Documento && u.Id != input.UsuarioId))
                return BadRequest("El documento ya está registrado en otro usuario");

            // 5. Validar correo si se proporciona
            if (!string.IsNullOrWhiteSpace(input.Correo))
            {
                if (await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo && u.Id != input.UsuarioId))
                    return BadRequest("Ya existe otro usuario con ese correo");
            }

            // Crear el barbero
            var barbero = new Barbero
            {
                UsuarioId = input.UsuarioId,
                Telefono = input.Telefono,
                Direccion = input.Direccion,
                Barrio = input.Barrio,
                FechaNacimiento = input.FechaNacimiento,
                Especialidad = input.Especialidad ?? "General",
                Estado = input.Estado,
                FechaContratacion = DateTime.Now
            };

            _context.Barberos.Add(barbero);
            await _context.SaveChangesAsync();

            // Retornar el barbero creado con su usuario
            var barberoCreado = await _context.Barberos
                .Include(b => b.Usuario)
                    .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(b => b.Id == barbero.Id);

            var barberoDto = new BarberoDto
            {
                Id = barberoCreado.Id,
                UsuarioId = barberoCreado.UsuarioId,
                Nombre = barberoCreado.Usuario.Nombre,
                Apellido = barberoCreado.Usuario.Apellido,
                Documento = barberoCreado.Usuario.Documento ?? "",
                Correo = barberoCreado.Usuario.Correo,
                Telefono = barberoCreado.Telefono,
                Direccion = barberoCreado.Direccion,
                Barrio = barberoCreado.Barrio,
                FechaNacimiento = barberoCreado.FechaNacimiento,
                Especialidad = barberoCreado.Especialidad,
                FotoPerfil = barberoCreado.Usuario.FotoPerfil,
                Estado = barberoCreado.Estado,
                FechaContratacion = barberoCreado.FechaContratacion,
                Usuario = new UsuarioDto
                {
                    Id = barberoCreado.Usuario.Id,
                    Nombre = barberoCreado.Usuario.Nombre,
                    Apellido = barberoCreado.Usuario.Apellido,
                    Correo = barberoCreado.Usuario.Correo,
                    RolId = barberoCreado.Usuario.RolId,
                    RolNombre = barberoCreado.Usuario.Rol?.Nombre,
                    Estado = barberoCreado.Usuario.Estado,
                    FechaCreacion = barberoCreado.Usuario.FechaCreacion
                }
            };

            return CreatedAtAction(nameof(GetById), new { id = barbero.Id }, barberoDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BarberoInput input)
        {
            if (input == null)
                return BadRequest("El objeto barbero es requerido");

            // Busca el barbero existente con su usuario
            var barberoExistente = await _context.Barberos
                .Include(b => b.Usuario)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (barberoExistente == null) 
                return NotFound();

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(input.Nombre))
                return BadRequest("El nombre es requerido");

            if (string.IsNullOrWhiteSpace(input.Apellido))
                return BadRequest("El apellido es requerido");

            if (string.IsNullOrWhiteSpace(input.Documento))
                return BadRequest("El documento es requerido");

            if (string.IsNullOrWhiteSpace(input.Correo))
                return BadRequest("El correo es requerido");

            // Validar documento si cambió
            if (input.Documento != barberoExistente.Usuario.Documento)
            {
                if (await _context.Usuarios.AnyAsync(u => u.Documento == input.Documento && u.Id != barberoExistente.UsuarioId))
                    return BadRequest("Ya existe otro barbero con ese documento");
            }

            // Validar correo si cambió
            if (input.Correo != barberoExistente.Usuario.Correo)
            {
                if (await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo && u.Id != barberoExistente.UsuarioId))
                    return BadRequest("Ya existe otro barbero con ese correo");
            }

            // Actualizar valores del barbero
            barberoExistente.Telefono = input.Telefono;
            barberoExistente.Direccion = input.Direccion;
            barberoExistente.Barrio = input.Barrio;
            barberoExistente.FechaNacimiento = input.FechaNacimiento;
            barberoExistente.Especialidad = input.Especialidad ?? "General";
            barberoExistente.Estado = input.Estado;

            // Actualizar valores del usuario
            var usuario = await _context.Usuarios.FindAsync(barberoExistente.UsuarioId);
            if (usuario != null)
            {
                // Validar URL de imagen usando el helper estandarizado
                if (!BarberiaApi.Helpers.ValidationHelper.ValidarUrlImagen(input.FotoPerfil, out var imgError))
                {
                    return BadRequest(imgError);
                }

                usuario.Nombre = input.Nombre;
                usuario.Apellido = input.Apellido;
                usuario.Documento = input.Documento;
                usuario.Correo = input.Correo;
                usuario.FotoPerfil = input.FotoPerfil;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Barberos.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult<CambioEstadoResponse<BarberoDto>>> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        {
            var barbero = await _context.Barberos
                .Include(b => b.Usuario)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (barbero == null) 
                return NotFound();

            // Actualizar el estado del barbero y del usuario vinculado
            barbero.Estado = input.estado;
            if (barbero.Usuario != null) barbero.Usuario.Estado = input.estado;
            await _context.SaveChangesAsync();

            var barberoDto = new BarberoDto
            {
                Id = barbero.Id,
                UsuarioId = barbero.UsuarioId,
                Nombre = barbero.Usuario.Nombre,
                Apellido = barbero.Usuario.Apellido,
                Documento = barbero.Usuario.Documento ?? "",
                Correo = barbero.Usuario.Correo,
                Telefono = barbero.Telefono,
                Direccion = barbero.Direccion,
                Barrio = barbero.Barrio,
                FechaNacimiento = barbero.FechaNacimiento,
                Especialidad = barbero.Especialidad,
                FotoPerfil = barbero.Usuario.FotoPerfil,
                Estado = barbero.Estado,
                FechaContratacion = barbero.FechaContratacion,
                Usuario = new UsuarioDto
                {
                    Id = barbero.Usuario.Id,
                    Nombre = barbero.Usuario.Nombre,
                    Apellido = barbero.Usuario.Apellido,
                    Correo = barbero.Usuario.Correo,
                    RolId = barbero.Usuario.RolId,
                    RolNombre = barbero.Usuario.Rol?.Nombre,
                    Estado = barbero.Usuario.Estado,
                    FechaCreacion = barbero.Usuario.FechaCreacion
                }
            };

            var response = new CambioEstadoResponse<BarberoDto>
            {
                entidad = barberoDto,
                mensaje = input.estado ? "Barbero activado exitosamente" : "Barbero desactivado exitosamente",
                exitoso = true
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var barbero = await _context.Barberos
                .Include(b => b.Agendamientos)
                .Include(b => b.EntregasInsumos)
                .Include(b => b.Usuario)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (barbero == null) return NotFound();
            var usuario = barbero.Usuario;
            bool tieneAgendamientosActivos = barbero.Agendamientos.Any(a => a.Estado != "Cancelada");
            bool tieneEntregas = barbero.EntregasInsumos.Any();
            bool tieneVentasComoBarbero = await _context.Ventas.AnyAsync(v => v.BarberoId == barbero.Id);
            bool tieneRegistroUsuario = await _context.Compras.AnyAsync(c => c.UsuarioId == usuario.Id)
                                        || await _context.Devoluciones.AnyAsync(d => d.UsuarioId == usuario.Id)
                                        || await _context.EntregasInsumos.AnyAsync(e => e.UsuarioId == usuario.Id)
                                        || await _context.Ventas.AnyAsync(v => v.UsuarioId == usuario.Id);
            if (tieneAgendamientosActivos || tieneEntregas || tieneVentasComoBarbero || tieneRegistroUsuario)
            {
                barbero.Estado = false;
                if (usuario != null) usuario.Estado = false;
                await _context.SaveChangesAsync();
                return Ok(new {
                    message = "Barbero y usuario desactivados (historial asociado)",
                    eliminado = true,
                    fisico = false,
                    motivos = new {
                        agendamientosActivos = barbero.Agendamientos.Count(a => a.Estado != "Cancelada"),
                        entregasAsociadas = barbero.EntregasInsumos.Count(),
                        ventasComoBarbero = tieneVentasComoBarbero,
                        registrosUsuario = tieneRegistroUsuario
                    }
                });
            }
            _context.Agendamientos.RemoveRange(barbero.Agendamientos.Where(a => a.Estado == "Cancelada"));
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (usuario != null) _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return Ok(new {
                    message = "Usuario y barbero eliminados físicamente",
                    eliminado = true,
                    fisico = true
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}
