using BarberiaApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System;
using BarberiaApi.Helpers;
using BarberiaApi.Services;
using System.IO;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly BarberiaContext _context;
        private readonly IPhotoService _photoService;
        public UsuariosController(BarberiaContext context, IPhotoService photoService)
        {
            _context = context;
            _photoService = photoService;
        }

        [HttpGet("analisis")]
        public async Task<ActionResult<object>> Analisis([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var q = _context.Usuarios
                .Select(u => new AnalisisUsuarioDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Correo = u.Correo,
                    RolId = u.RolId,
                    RolNombre = u.Rol != null ? u.Rol.Nombre : null,
                    EsCliente = u.Cliente != null,
                    EsBarbero = u.Barbero != null,
                    VentasHechas = u.Ventas.Count(),
                    ComprasHechas = u.Compras.Count(),
                    DevolucionesProcesadas = u.Devoluciones.Count(),
                    EntregasRegistradas = u.EntregasInsumos.Count(),
                    VentasComoCliente = u.Cliente != null ? u.Cliente.Venta.Count() : 0,
                    AgendamientosCliente = u.Cliente != null ? u.Cliente.Agendamientos.Count() : 0,
                    DevolucionesCliente = u.Cliente != null ? u.Cliente.Devoluciones.Count() : 0,
                    AgendamientosBarbero = u.Barbero != null ? u.Barbero.Agendamientos.Count() : 0,
                    EntregasBarbero = u.Barbero != null ? u.Barbero.EntregasInsumos.Count() : 0,
                    ModulosAcceso = u.Rol != null
                        ? u.Rol.RolesModulos.Where(rm => rm.PuedeVer == true && rm.Modulo != null).Select(rm => rm.Modulo!.Nombre).ToList()
                        : new List<string>()
                });
            var totalCount = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("{id}/analisis")]
        public async Task<ActionResult<AnalisisUsuarioDto>> AnalisisPorId(int id)
        {
            var data = await _context.Usuarios
                .Where(u => u.Id == id)
                .Select(u => new AnalisisUsuarioDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Correo = u.Correo,
                    RolId = u.RolId,
                    RolNombre = u.Rol != null ? u.Rol.Nombre : null,
                    EsCliente = u.Cliente != null,
                    EsBarbero = u.Barbero != null,
                    VentasHechas = u.Ventas.Count(),
                    ComprasHechas = u.Compras.Count(),
                    DevolucionesProcesadas = u.Devoluciones.Count(),
                    EntregasRegistradas = u.EntregasInsumos.Count(),
                    VentasComoCliente = u.Cliente != null ? u.Cliente.Venta.Count() : 0,
                    AgendamientosCliente = u.Cliente != null ? u.Cliente.Agendamientos.Count() : 0,
                    DevolucionesCliente = u.Cliente != null ? u.Cliente.Devoluciones.Count() : 0,
                    AgendamientosBarbero = u.Barbero != null ? u.Barbero.Agendamientos.Count() : 0,
                    EntregasBarbero = u.Barbero != null ? u.Barbero.EntregasInsumos.Count() : 0,
                    ModulosAcceso = u.Rol != null
                        ? u.Rol.RolesModulos.Where(rm => rm.PuedeVer == true && rm.Modulo != null).Select(rm => rm.Modulo!.Nombre).ToList()
                        : new List<string>()
                })
                .FirstOrDefaultAsync();
            if (data == null) return NotFound();
            return Ok(data);
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            var baseQ = _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Cliente)
                .Include(u => u.Barbero)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                baseQ = baseQ.Where(u =>
                    (u.Nombre != null && u.Nombre.ToLower().Contains(term)) ||
                    (u.Apellido != null && u.Apellido.ToLower().Contains(term)) ||
                    (u.Documento != null && u.Documento.ToLower().Contains(term)) ||
                    (u.Correo != null && u.Correo.ToLower().Contains(term)) ||
                    (u.Cliente != null && (
                        (u.Cliente.Telefono != null && u.Cliente.Telefono.ToLower().Contains(term)) ||
                        (u.Cliente.Direccion != null && u.Cliente.Direccion.ToLower().Contains(term)) ||
                        (u.Cliente.Barrio != null && u.Cliente.Barrio.ToLower().Contains(term))
                    )) ||
                    (u.Barbero != null && (
                        (u.Barbero.Telefono != null && u.Barbero.Telefono.ToLower().Contains(term)) ||
                        (u.Barbero.Direccion != null && u.Barbero.Direccion.ToLower().Contains(term)) ||
                        (u.Barbero.Barrio != null && u.Barbero.Barrio.ToLower().Contains(term)) ||
                        (u.Barbero.Especialidad != null && u.Barbero.Especialidad.ToLower().Contains(term))
                    ))
                );
            }
            var totalCount = await baseQ.CountAsync();
            var items = await baseQ
                .OrderBy(u => u.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UsuarioDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Correo = u.Correo,
                    RolId = u.RolId,
                    RolNombre = u.Rol != null ? u.Rol.Nombre : null,
                    TipoDocumento = u.TipoDocumento,
                    Documento = u.Documento,
                    Telefono = u.Cliente != null ? u.Cliente.Telefono : (u.Barbero != null ? u.Barbero.Telefono : null),
                    Direccion = u.Cliente != null ? u.Cliente.Direccion : (u.Barbero != null ? u.Barbero.Direccion : null),
                    Barrio = u.Cliente != null ? u.Cliente.Barrio : (u.Barbero != null ? u.Barbero.Barrio : null),
                    FechaNacimiento = u.Cliente != null ? u.Cliente.FechaNacimiento : (u.Barbero != null ? u.Barbero.FechaNacimiento : null),
                    FotoPerfil = u.FotoPerfil,
                    Estado = u.Estado,
                    FechaCreacion = u.FechaCreacion,
                    FechaModificacion = u.FechaModificacion,
                    Cliente = u.Cliente != null ? new ClienteDto
                    {
                        Id = u.Cliente.Id,
                        UsuarioId = u.Cliente.UsuarioId,
                        Nombre = u.Nombre,
                        Apellido = u.Apellido,
                        Documento = u.Documento ?? "",
                        Correo = u.Correo,
                        Telefono = u.Cliente.Telefono,
                        Direccion = u.Cliente.Direccion,
                        Barrio = u.Cliente.Barrio,
                        FechaNacimiento = u.Cliente.FechaNacimiento,
                        FotoPerfil = u.FotoPerfil,
                        Estado = u.Cliente.Estado,
                        FechaRegistro = u.Cliente.FechaRegistro
                    } : null,
                    Barbero = u.Barbero != null ? new BarberoDto
                    {
                        Id = u.Barbero.Id,
                        UsuarioId = u.Barbero.UsuarioId,
                        Nombre = u.Nombre,
                        Apellido = u.Apellido,
                        Documento = u.Documento ?? "",
                        Correo = u.Correo,
                        Telefono = u.Barbero.Telefono,
                        Direccion = u.Barbero.Direccion,
                        Barrio = u.Barbero.Barrio,
                        FechaNacimiento = u.Barbero.FechaNacimiento,
                        Especialidad = u.Barbero.Especialidad,
                        FotoPerfil = u.FotoPerfil,
                        Estado = u.Barbero.Estado,
                        FechaContratacion = u.Barbero.FechaContratacion
                    } : null
                })
                .ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioDto>> GetById(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Cliente)
                .Include(u => u.Barbero)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null) 
                return NotFound();

            var usuarioDto = new UsuarioDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Correo = usuario.Correo,
                RolId = usuario.RolId,
                RolNombre = usuario.Rol?.Nombre,
                TipoDocumento = usuario.TipoDocumento,
                Documento = usuario.Documento,
                Telefono = usuario.Cliente != null ? usuario.Cliente.Telefono : (usuario.Barbero != null ? usuario.Barbero.Telefono : null),
                Direccion = usuario.Cliente != null ? usuario.Cliente.Direccion : (usuario.Barbero != null ? usuario.Barbero.Direccion : null),
                Barrio = usuario.Cliente != null ? usuario.Cliente.Barrio : (usuario.Barbero != null ? usuario.Barbero.Barrio : null),
                FechaNacimiento = usuario.Cliente != null ? usuario.Cliente.FechaNacimiento : (usuario.Barbero != null ? usuario.Barbero.FechaNacimiento : null),
                FotoPerfil = usuario.FotoPerfil,
                Estado = usuario.Estado,
                FechaCreacion = usuario.FechaCreacion,
                FechaModificacion = usuario.FechaModificacion,
                Cliente = usuario.Cliente != null ? new ClienteDto
                {
                    Id = usuario.Cliente.Id,
                    UsuarioId = usuario.Cliente.UsuarioId,
                    Nombre = usuario.Nombre,
                    Apellido = usuario.Apellido,
                    Documento = usuario.Documento ?? "",
                    Correo = usuario.Correo,
                    Telefono = usuario.Cliente.Telefono,
                    Direccion = usuario.Cliente.Direccion,
                    Barrio = usuario.Cliente.Barrio,
                    FechaNacimiento = usuario.Cliente.FechaNacimiento,
                    FotoPerfil = usuario.FotoPerfil,
                    Estado = usuario.Cliente.Estado,
                    FechaRegistro = usuario.Cliente.FechaRegistro
                } : null,
                Barbero = usuario.Barbero != null ? new BarberoDto
                {
                    Id = usuario.Barbero.Id,
                    UsuarioId = usuario.Barbero.UsuarioId,
                    Nombre = usuario.Nombre,
                    Apellido = usuario.Apellido,
                    Documento = usuario.Documento ?? "",
                    Correo = usuario.Correo,
                    Telefono = usuario.Barbero.Telefono,
                    Direccion = usuario.Barbero.Direccion,
                    Barrio = usuario.Barbero.Barrio,
                    FechaNacimiento = usuario.Barbero.FechaNacimiento,
                    Especialidad = usuario.Barbero.Especialidad,
                    FotoPerfil = usuario.FotoPerfil,
                    Estado = usuario.Barbero.Estado,
                    FechaContratacion = usuario.Barbero.FechaContratacion
                } : null
            };

            return Ok(usuarioDto);
        }

        [HttpPost("{id}/foto")]
        [RequestSizeLimit(15728640)]
        public async Task<ActionResult<object>> SubirFoto(int id, IFormFile imagen)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null) return NotFound();
            if (imagen == null || imagen.Length == 0) return BadRequest("Imagen requerida");
            if (string.IsNullOrWhiteSpace(imagen.ContentType) || !imagen.ContentType.StartsWith("image/")) return BadRequest("Content-Type inválido");
            var res = await _photoService.AddPhotoAsync(imagen);
            if (res.Error != null) return BadRequest(res.Error.Message);
            var url = res.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(url)) return BadRequest("Error al subir");
            usuario.FotoPerfil = url;
            usuario.FechaModificacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { url, publicId = res.PublicId });
        }

        [HttpDelete("{id}/foto")]
        public async Task<ActionResult<object>> EliminarFoto(int id, [FromQuery] bool borrarCloud = true)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null) return NotFound();
            var url = usuario.FotoPerfil;
            if (string.IsNullOrWhiteSpace(url)) return Ok(new { eliminado = false });
            string publicId = borrarCloud ? ExtraerPublicIdDesdeUrl(url) ?? "" : "";
            if (borrarCloud && !string.IsNullOrWhiteSpace(publicId)) await _photoService.DeletePhotoAsync(publicId);
            usuario.FotoPerfil = null;
            usuario.FechaModificacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { eliminado = true, publicId });
        }

        private static string? ExtraerPublicIdDesdeUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var marker = "/upload/";
                var idx = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return null;
                var after = path[(idx + marker.Length)..].Trim('/');
                var segments = after.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 0) return null;
                var start = 0;
                if (segments[0].Length > 1 && segments[0][0] == 'v' && long.TryParse(segments[0][1..], out _)) start = 1;
                if (start >= segments.Length) return null;
                var last = segments[^1];
                var nameNoExt = Path.GetFileNameWithoutExtension(last);
                var leading = segments.Length - start > 1 ? string.Join('/', segments[start..^1]) + "/" : string.Empty;
                return leading + nameNoExt;
            }
            catch
            {
                return null;
            }
        }

        [HttpPost]
        public async Task<ActionResult<UsuarioDto>> Create([FromBody] UsuarioInput input)
        {
            if (input == null)
                return BadRequest("El objeto usuario es requerido");

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(input.Nombre))
                return BadRequest("El nombre es requerido");

            if (string.IsNullOrWhiteSpace(input.Apellido))
                return BadRequest("El apellido es requerido");

            if (string.IsNullOrWhiteSpace(input.Correo))
                return BadRequest("El correo es requerido");

            if (string.IsNullOrWhiteSpace(input.Contrasena))
                return BadRequest("La contraseña es requerida");

            // Validar que el correo no exista
            if (await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo))
                return BadRequest("Ya existe un usuario con ese correo");

            // Validar documento si se proporciona
            if (!string.IsNullOrWhiteSpace(input.Documento))
            {
                if (await _context.Usuarios.AnyAsync(u => u.Documento == input.Documento))
                    return BadRequest("Ya existe un usuario con ese documento");
            }

            // Validar rol
            var rol = await _context.Roles.FindAsync(input.RolId);
            if (rol == null)
                return BadRequest("El rol especificado no existe");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validar URL de imagen usando el helper estandarizado
                if (!BarberiaApi.Helpers.ValidationHelper.ValidarUrlImagen(input.FotoPerfil, out var imgError))
                {
                    return BadRequest(imgError);
                }

                // Crear el usuario base
                var usuario = new Usuario
                {
                    Nombre = input.Nombre,
                    Apellido = input.Apellido,
                    Correo = input.Correo,
                    Contrasena = input.Contrasena, // TODO: Implementar hash de contraseña
                    RolId = input.RolId,
                    TipoDocumento = input.TipoDocumento,
                    Documento = input.Documento,
                    FotoPerfil = input.FotoPerfil,
                    Estado = input.Estado,
                    FechaCreacion = DateTime.Now
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // LÓGICA AUTOMÁTICA: Crear perfil según Rol
                if (input.RolId == 3) // Cliente
                {
                    var cliente = new Cliente
                    {
                        UsuarioId = usuario.Id,
                        Telefono = input.Telefono,
                        Direccion = input.Direccion,
                        Barrio = input.Barrio,
                        FechaNacimiento = input.FechaNacimiento,
                        Estado = true,
                        FechaRegistro = DateTime.Now
                    };
                    _context.Clientes.Add(cliente);
                }
                else if (input.RolId == 2) // Barbero
                {
                    var barbero = new Barbero
                    {
                        UsuarioId = usuario.Id,
                        Telefono = input.Telefono,
                        Direccion = input.Direccion,
                        Barrio = input.Barrio,
                        FechaNacimiento = input.FechaNacimiento,
                        Especialidad = "General",
                        Estado = true,
                        FechaContratacion = DateTime.Now
                    };
                    _context.Barberos.Add(barbero);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, await MapToDto(usuario.Id));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // Se removió el flujo de envío de correos y restablecimiento de contraseña por solicitud

        private async Task<UsuarioDto> MapToDto(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Cliente)
                .Include(u => u.Barbero)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
                throw new Exception("Usuario no encontrado");

            return new UsuarioDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Correo = usuario.Correo,
                RolId = usuario.RolId,
                RolNombre = usuario.Rol?.Nombre,
                TipoDocumento = usuario.TipoDocumento,
                Documento = usuario.Documento,
                Telefono = usuario.Cliente != null ? usuario.Cliente.Telefono : (usuario.Barbero != null ? usuario.Barbero.Telefono : null),
                Direccion = usuario.Cliente != null ? usuario.Cliente.Direccion : (usuario.Barbero != null ? usuario.Barbero.Direccion : null),
                Barrio = usuario.Cliente != null ? usuario.Cliente.Barrio : (usuario.Barbero != null ? usuario.Barbero.Barrio : null),
                FechaNacimiento = usuario.Cliente != null ? usuario.Cliente.FechaNacimiento : (usuario.Barbero != null ? usuario.Barbero.FechaNacimiento : null),
                FotoPerfil = usuario.FotoPerfil,
                Estado = usuario.Estado,
                FechaCreacion = usuario.FechaCreacion,
                FechaModificacion = usuario.FechaModificacion,
                Cliente = usuario.Cliente != null ? new ClienteDto
                {
                    Id = usuario.Cliente.Id,
                    UsuarioId = usuario.Cliente.UsuarioId,
                    Nombre = usuario.Nombre,
                    Apellido = usuario.Apellido,
                    Documento = usuario.Documento ?? "",
                    Correo = usuario.Correo,
                    Telefono = usuario.Cliente.Telefono,
                    Direccion = usuario.Cliente.Direccion,
                    Barrio = usuario.Cliente.Barrio,
                    FechaNacimiento = usuario.Cliente.FechaNacimiento,
                    FotoPerfil = usuario.FotoPerfil,
                    Estado = usuario.Cliente.Estado,
                    FechaRegistro = usuario.Cliente.FechaRegistro
                } : null,
                Barbero = usuario.Barbero != null ? new BarberoDto
                {
                    Id = usuario.Barbero.Id,
                    UsuarioId = usuario.Barbero.UsuarioId,
                    Nombre = usuario.Nombre,
                    Apellido = usuario.Apellido,
                    Documento = usuario.Documento ?? "",
                    Correo = usuario.Correo,
                    Telefono = usuario.Barbero.Telefono,
                    Direccion = usuario.Barbero.Direccion,
                    Barrio = usuario.Barbero.Barrio,
                    FechaNacimiento = usuario.Barbero.FechaNacimiento,
                    Especialidad = usuario.Barbero.Especialidad,
                    FotoPerfil = usuario.FotoPerfil,
                    Estado = usuario.Barbero.Estado,
                    FechaContratacion = usuario.Barbero.FechaContratacion
                } : null
            };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UsuarioInput input)
        {
            if (input == null)
                return BadRequest("El objeto usuario es requerido");

            // Busca el usuario existente con sus perfiles
            var usuarioExistente = await _context.Usuarios
                .Include(u => u.Cliente)
                .Include(u => u.Barbero)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuarioExistente == null) 
                return NotFound();

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(input.Nombre))
                return BadRequest("El nombre es requerido");

            if (string.IsNullOrWhiteSpace(input.Apellido))
                return BadRequest("El apellido es requerido");

            if (string.IsNullOrWhiteSpace(input.Correo))
                return BadRequest("El correo es requerido");

            // Validar correo si cambió
            if (input.Correo != usuarioExistente.Correo)
            {
                if (await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo && u.Id != id))
                    return BadRequest("Ya existe otro usuario con ese correo");
            }

            // Validar documento si cambió
            if (!string.IsNullOrWhiteSpace(input.Documento) && input.Documento != usuarioExistente.Documento)
            {
                if (await _context.Usuarios.AnyAsync(u => u.Documento == input.Documento && u.Id != id))
                    return BadRequest("Ya existe otro usuario con ese documento");
            }

            // Validar rol si cambió
            if (input.RolId != usuarioExistente.RolId)
            {
                var rol = await _context.Roles.FindAsync(input.RolId);
                if (rol == null)
                    return BadRequest("El rol especificado no existe");
            }

            // Validar URL de imagen usando el helper estandarizado
            if (!BarberiaApi.Helpers.ValidationHelper.ValidarUrlImagen(input.FotoPerfil, out var imgErrorUpdate))
            {
                return BadRequest(imgErrorUpdate);
            }

            // Actualizar valores
            usuarioExistente.Nombre = input.Nombre;
            usuarioExistente.Apellido = input.Apellido;
            usuarioExistente.Correo = input.Correo;
            if (!string.IsNullOrWhiteSpace(input.Contrasena))
                usuarioExistente.Contrasena = input.Contrasena; // TODO: Implementar hash de contraseña
            usuarioExistente.RolId = input.RolId;
            usuarioExistente.TipoDocumento = input.TipoDocumento;
            usuarioExistente.Documento = input.Documento;
            usuarioExistente.FotoPerfil = input.FotoPerfil;
            usuarioExistente.Estado = input.Estado;
            usuarioExistente.FechaModificacion = DateTime.Now;

            // Actualizar perfil si existe
            if (usuarioExistente.Cliente != null)
            {
                usuarioExistente.Cliente.Telefono = input.Telefono;
                usuarioExistente.Cliente.Direccion = input.Direccion;
                usuarioExistente.Cliente.Barrio = input.Barrio;
                usuarioExistente.Cliente.FechaNacimiento = input.FechaNacimiento;
            }
            if (usuarioExistente.Barbero != null)
            {
                usuarioExistente.Barbero.Telefono = input.Telefono;
                usuarioExistente.Barbero.Direccion = input.Direccion;
                usuarioExistente.Barbero.Barrio = input.Barrio;
                usuarioExistente.Barbero.FechaNacimiento = input.FechaNacimiento;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Usuarios.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult<CambioEstadoResponse<Usuario>>> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Cliente)
                .Include(u => u.Barbero)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null) return NotFound();

            // Actualizar solo el estado
            usuario.Estado = input.estado;
            if (usuario.Cliente != null) usuario.Cliente.Estado = input.estado;
            if (usuario.Barbero != null) usuario.Barbero.Estado = input.estado;
            await _context.SaveChangesAsync();

            var response = new CambioEstadoResponse<Usuario>
            {
                entidad = usuario,
                mensaje = input.estado ? "Usuario activado exitosamente" : "Usuario desactivado exitosamente",
                exitoso = true
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Cliente)
                .Include(u => u.Barbero)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null) return NotFound();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var fallback = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == "system@local");
                if (fallback == null)
                {
                    var rolAdmin = await _context.Roles.FirstOrDefaultAsync(r => r.Id == 1);
                    fallback = new Usuario
                    {
                        Nombre = "Sistema",
                        Apellido = "Local",
                        Correo = "system@local",
                        Contrasena = Guid.NewGuid().ToString(),
                        RolId = rolAdmin?.Id ?? 1,
                        Estado = true,
                        FechaCreacion = DateTime.Now
                    };
                    _context.Usuarios.Add(fallback);
                    await _context.SaveChangesAsync();
                }
                var ventas = await _context.Ventas.Where(v => v.UsuarioId == id).ToListAsync();
                foreach (var v in ventas) v.UsuarioId = fallback.Id;
                var compras = await _context.Compras.Where(c => c.UsuarioId == id).ToListAsync();
                foreach (var c in compras) c.UsuarioId = fallback.Id;
                var entregas = await _context.EntregasInsumos.Where(e => e.UsuarioId == id).ToListAsync();
                foreach (var e in entregas) e.UsuarioId = fallback.Id;
                var devols = await _context.Devoluciones.Where(d => d.UsuarioId == id).ToListAsync();
                foreach (var d in devols) d.UsuarioId = fallback.Id;
                if (usuario.Cliente != null)
                {
                    var clienteId = usuario.Cliente.Id;
                    var agsCliente = await _context.Agendamientos.Where(a => a.ClienteId == clienteId).ToListAsync();
                    _context.Agendamientos.RemoveRange(agsCliente);
                    var devsCliente = await _context.Devoluciones.Where(d => d.ClienteId == clienteId).ToListAsync();
                    foreach (var d in devsCliente) d.ClienteId = null;
                }
                if (usuario.Barbero != null)
                {
                    var barberoId = usuario.Barbero.Id;
                    var agsBarbero = await _context.Agendamientos.Where(a => a.BarberoId == barberoId).ToListAsync();
                    _context.Agendamientos.RemoveRange(agsBarbero);
                    var entBarbero = await _context.EntregasInsumos.Where(e => e.BarberoId == barberoId).Include(e => e.DetalleEntregasInsumos).ToListAsync();
                    foreach (var e in entBarbero) _context.DetalleEntregasInsumos.RemoveRange(e.DetalleEntregasInsumos);
                    _context.EntregasInsumos.RemoveRange(entBarbero);
                    var devsBarbero = await _context.Devoluciones.Where(d => d.BarberoId == barberoId).ToListAsync();
                    foreach (var d in devsBarbero) d.BarberoId = null;
                }
                if (usuario.Cliente != null) _context.Clientes.Remove(usuario.Cliente);
                if (usuario.Barbero != null) _context.Barberos.Remove(usuario.Barbero);
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return Ok(new { message = "Usuario eliminado permanentemente", eliminado = true, fisico = true });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}
