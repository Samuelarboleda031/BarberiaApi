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
    public class ClientesController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public ClientesController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClienteDto>>> GetAll()
        {
            var clientes = await _context.Clientes
                .Include(c => c.Usuario)
                .Select(c => new ClienteDto
                {
                    Id = c.Id,
                    UsuarioId = c.UsuarioId,
                    Nombre = c.Usuario.Nombre,
                    Apellido = c.Usuario.Apellido,
                    Documento = c.Usuario.Documento,
                    Correo = c.Usuario.Correo,
                    Telefono = c.Telefono,
                    Direccion = c.Direccion,
                    Barrio = c.Barrio,
                    FechaNacimiento = c.FechaNacimiento,
                    FotoPerfil = c.Usuario.FotoPerfil,
                    Estado = c.Estado,
                    FechaRegistro = c.FechaRegistro,
                    Usuario = new UsuarioDto
                    {
                        Id = c.Usuario.Id,
                        Nombre = c.Usuario.Nombre,
                        Apellido = c.Usuario.Apellido,
                        Correo = c.Usuario.Correo,
                        RolId = c.Usuario.RolId,
                        RolNombre = c.Usuario.Rol != null ? c.Usuario.Rol.Nombre : null,
                        Estado = c.Usuario.Estado,
                        FechaCreacion = c.Usuario.FechaCreacion
                    }
                })
                .ToListAsync();

            return Ok(clientes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ClienteDto>> GetById(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Usuario)
                    .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null) 
                return NotFound();

            var clienteDto = new ClienteDto
            {
                Id = cliente.Id,
                UsuarioId = cliente.UsuarioId,
                Nombre = cliente.Usuario.Nombre,
                Apellido = cliente.Usuario.Apellido,
                Documento = cliente.Usuario.Documento,
                Correo = cliente.Usuario.Correo,
                Telefono = cliente.Telefono,
                Direccion = cliente.Direccion,
                Barrio = cliente.Barrio,
                FechaNacimiento = cliente.FechaNacimiento,
                FotoPerfil = cliente.Usuario.FotoPerfil,
                Estado = cliente.Estado,
                FechaRegistro = cliente.FechaRegistro,
                Usuario = new UsuarioDto
                {
                    Id = cliente.Usuario.Id,
                    Nombre = cliente.Usuario.Nombre,
                    Apellido = cliente.Usuario.Apellido,
                    Correo = cliente.Usuario.Correo,
                    RolId = cliente.Usuario.RolId,
                    RolNombre = cliente.Usuario.Rol?.Nombre,
                    Estado = cliente.Usuario.Estado,
                    FechaCreacion = cliente.Usuario.FechaCreacion
                }
            };

            return Ok(clienteDto);
        }

        [HttpGet("{id}/saldo-disponible")]
        public async Task<ActionResult<object>> GetSaldoDisponible(int id)
        {
            var existe = await _context.Clientes.AnyAsync(c => c.Id == id);
            if (!existe) return NotFound();

            var totalDevoluciones = await _context.Devoluciones
                .Where(d => d.ClienteId == id && (d.Estado == "Activo" || d.Estado == "Completada" || d.Estado == "Procesado"))
                .SumAsync(d => d.SaldoAFavor ?? 0);

            var totalUsado = await _context.Ventas
                .Where(v => v.ClienteId == id && v.Estado != "Anulada")
                .SumAsync(v => v.SaldoAFavorUsado ?? 0);

            var disponible = Math.Max(0, totalDevoluciones - totalUsado);

            return Ok(new
            {
                clienteId = id,
                totalDevoluciones,
                totalUsado,
                disponible
            });
        }

        [HttpPost]
        public async Task<ActionResult<ClienteDto>> Create([FromBody] ClienteInput input)
        {
            if (input == null)
                return BadRequest("El objeto cliente es requerido");

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

            // 2. Validar que el usuario tenga el rol correcto (Cliente)
            if (usuario.RolId != 3)
                return BadRequest("El usuario no tiene un rol de Cliente");

            // 3. Validar que no exista ya un perfil de cliente para este usuario
            if (await _context.Clientes.AnyAsync(c => c.UsuarioId == input.UsuarioId))
                return BadRequest("Ya existe un perfil de cliente para este usuario");

            // Validar que el documento no exista
            if (await _context.Usuarios.AnyAsync(u => u.Documento == input.Documento))
                return BadRequest("El documento ya está registrado");

            // 5. Validar correo si se proporciona
            if (!string.IsNullOrWhiteSpace(input.Correo))
            {
                if (await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo))
                    return BadRequest("Ya existe un usuario con ese correo");
            }

            // Crear el cliente
            var cliente = new Cliente
            {
                UsuarioId = input.UsuarioId,
                Telefono = input.Telefono,
                Direccion = input.Direccion,
                Barrio = input.Barrio,
                FechaNacimiento = input.FechaNacimiento,
                Estado = input.Estado,
                FechaRegistro = DateTime.Now
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            // Retornar el cliente creado con su usuario
            var clienteCreado = await _context.Clientes
                .Include(c => c.Usuario)
                    .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(c => c.Id == cliente.Id);

            var clienteDto = new ClienteDto
            {
                Id = clienteCreado.Id,
                UsuarioId = clienteCreado.UsuarioId,
                Nombre = clienteCreado.Usuario.Nombre,
                Apellido = clienteCreado.Usuario.Apellido,
                Documento = clienteCreado.Usuario.Documento,
                Correo = clienteCreado.Usuario.Correo,
                Telefono = clienteCreado.Telefono,
                Direccion = clienteCreado.Direccion,
                Barrio = clienteCreado.Barrio,
                FechaNacimiento = clienteCreado.FechaNacimiento,
                FotoPerfil = clienteCreado.Usuario.FotoPerfil,
                Estado = clienteCreado.Estado,
                FechaRegistro = clienteCreado.FechaRegistro,
                Usuario = new UsuarioDto
                {
                    Id = clienteCreado.Usuario.Id,
                    Nombre = clienteCreado.Usuario.Nombre,
                    Apellido = clienteCreado.Usuario.Apellido,
                    Correo = clienteCreado.Usuario.Correo,
                    RolId = clienteCreado.Usuario.RolId,
                    RolNombre = clienteCreado.Usuario.Rol?.Nombre,
                    Estado = clienteCreado.Usuario.Estado,
                    FechaCreacion = clienteCreado.Usuario.FechaCreacion
                }
            };

            return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, clienteDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ClienteInput input)
        {
            if (input == null)
                return BadRequest("El objeto cliente es requerido");

            // Busca el cliente existente con su usuario
            var clienteExistente = await _context.Clientes
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (clienteExistente == null) 
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
            if (input.Documento != clienteExistente.Usuario.Documento)
            {
                if (await _context.Usuarios.AnyAsync(u => u.Documento == input.Documento && u.Id != clienteExistente.UsuarioId))
                    return BadRequest("Ya existe otro usuario con ese documento");
            }

            // Validar correo si cambió
            if (input.Correo != clienteExistente.Usuario.Correo)
            {
                if (await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo && u.Id != clienteExistente.UsuarioId))
                    return BadRequest("Ya existe otro usuario con ese correo");
            }

            // Actualizar valores del cliente
            clienteExistente.Telefono = input.Telefono;
            clienteExistente.Direccion = input.Direccion;
            clienteExistente.Barrio = input.Barrio;
            clienteExistente.FechaNacimiento = input.FechaNacimiento;
            clienteExistente.Estado = input.Estado;

            // Actualizar valores del usuario
            var usuario = await _context.Usuarios.FindAsync(clienteExistente.UsuarioId);
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
                if (!await _context.Clientes.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult<CambioEstadoResponse<ClienteDto>>> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null) 
                return NotFound();

            // Actualizar solo el estado
            cliente.Estado = input.estado;
            await _context.SaveChangesAsync();

            var clienteDto = new ClienteDto
            {
                Id = cliente.Id,
                UsuarioId = cliente.UsuarioId,
                Nombre = cliente.Usuario.Nombre,
                Apellido = cliente.Usuario.Apellido,
                Documento = cliente.Usuario.Documento,
                Correo = cliente.Usuario.Correo,
                Telefono = cliente.Telefono,
                Direccion = cliente.Direccion,
                Barrio = cliente.Barrio,
                FechaNacimiento = cliente.FechaNacimiento,
                FotoPerfil = cliente.Usuario.FotoPerfil,
                Estado = cliente.Estado,
                FechaRegistro = cliente.FechaRegistro,
                Usuario = new UsuarioDto
                {
                    Id = cliente.Usuario.Id,
                    Nombre = cliente.Usuario.Nombre,
                    Apellido = cliente.Usuario.Apellido,
                    Correo = cliente.Usuario.Correo,
                    RolId = cliente.Usuario.RolId,
                    RolNombre = cliente.Usuario.Rol?.Nombre,
                    Estado = cliente.Usuario.Estado,
                    FechaCreacion = cliente.Usuario.FechaCreacion
                }
            };

            var response = new CambioEstadoResponse<ClienteDto>
            {
                entidad = clienteDto,
                mensaje = input.estado ? "Cliente activado exitosamente" : "Cliente desactivado exitosamente",
                exitoso = true
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Busca el cliente sin importar el estado
            var cliente = await _context.Clientes
                .Include(c => c.Agendamientos)
                .Include(c => c.Venta)
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            // Solo falla si realmente NO existe en la BD
            if (cliente == null) return NotFound();

            // Verificar si tiene agendamientos activos o ventas completadas
            bool tieneAgendamientosActivos = cliente.Agendamientos.Any(a => a.Estado != "Cancelada");
            bool tieneVentasCompletadas = cliente.Venta.Any(v => v.Estado == "Completada");

            if (tieneAgendamientosActivos || tieneVentasCompletadas)
            {
                // Soft Delete: Cambia el estado a false
                cliente.Estado = false;
                await _context.SaveChangesAsync();
                return Ok(new { 
                    message = "Cliente desactivado (borrado lógico por tener registros asociados)", 
                    eliminado = true, 
                    fisico = false,
                    motivo = tieneAgendamientosActivos ? "Agendamientos activos" : "Ventas completadas",
                    agendamientosActivos = cliente.Agendamientos.Count(a => a.Estado != "Cancelada"),
                    ventasCompletadas = cliente.Venta.Count(v => v.Estado == "Completada")
                });
            }

            // Borrado Físico: No tiene registros críticos
            // Eliminar dependencias primero
            _context.Agendamientos.RemoveRange(cliente.Agendamientos.Where(a => a.Estado == "Cancelada"));
            _context.Ventas.RemoveRange(cliente.Venta.Where(v => v.Estado != "Completada"));

            // Eliminar el cliente
            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
            return Ok(new { 
                message = "Cliente eliminado físicamente de la base de datos", 
                eliminado = true, 
                fisico = true 
            });
        }
    }
}
