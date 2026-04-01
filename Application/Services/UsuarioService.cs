using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly BarberiaContext _context;
    private readonly IPhotoService _photoService;

    public UsuarioService(BarberiaContext context, IPhotoService photoService)
    {
        _context = context;
        _photoService = photoService;
    }

    public async Task<ServiceResult<object>> AnalisisAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;
        var q = _context.Usuarios
            .AsNoTracking()
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
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> AnalisisPorIdAsync(int id)
    {
        var data = await _context.Usuarios
            .AsNoTracking()
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
        if (data == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(data);
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;
        var baseQ = _context.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.Cliente)
            .Include(u => u.Barbero)
            .AsNoTracking()
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
                    Id = u.Cliente.Id, UsuarioId = u.Cliente.UsuarioId, Nombre = u.Nombre, Apellido = u.Apellido,
                    Documento = u.Documento ?? "", Correo = u.Correo, Telefono = u.Cliente.Telefono,
                    Direccion = u.Cliente.Direccion, Barrio = u.Cliente.Barrio, FechaNacimiento = u.Cliente.FechaNacimiento,
                    FotoPerfil = u.FotoPerfil, Estado = u.Cliente.Estado, FechaRegistro = u.Cliente.FechaRegistro
                } : null,
                Barbero = u.Barbero != null ? new BarberoDto
                {
                    Id = u.Barbero.Id, UsuarioId = u.Barbero.UsuarioId, Nombre = u.Nombre, Apellido = u.Apellido,
                    Documento = u.Documento ?? "", Correo = u.Correo, Telefono = u.Barbero.Telefono,
                    Direccion = u.Barbero.Direccion, Barrio = u.Barbero.Barrio, FechaNacimiento = u.Barbero.FechaNacimiento,
                    Especialidad = u.Barbero.Especialidad, FotoPerfil = u.FotoPerfil, Estado = u.Barbero.Estado,
                    FechaContratacion = u.Barbero.FechaContratacion
                } : null
            })
            .ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .Include(u => u.Rol)
            .Include(u => u.Cliente)
            .Include(u => u.Barbero)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario == null)
            return ServiceResult<object>.NotFound();

        return ServiceResult<object>.Ok(MapUsuarioToDto(usuario));
    }

    private static UsuarioDto MapUsuarioToDto(Usuario usuario)
    {
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
                Id = usuario.Cliente.Id, UsuarioId = usuario.Cliente.UsuarioId, Nombre = usuario.Nombre, Apellido = usuario.Apellido,
                Documento = usuario.Documento ?? "", Correo = usuario.Correo, Telefono = usuario.Cliente.Telefono,
                Direccion = usuario.Cliente.Direccion, Barrio = usuario.Cliente.Barrio, FechaNacimiento = usuario.Cliente.FechaNacimiento,
                FotoPerfil = usuario.FotoPerfil, Estado = usuario.Cliente.Estado, FechaRegistro = usuario.Cliente.FechaRegistro
            } : null,
            Barbero = usuario.Barbero != null ? new BarberoDto
            {
                Id = usuario.Barbero.Id, UsuarioId = usuario.Barbero.UsuarioId, Nombre = usuario.Nombre, Apellido = usuario.Apellido,
                Documento = usuario.Documento ?? "", Correo = usuario.Correo, Telefono = usuario.Barbero.Telefono,
                Direccion = usuario.Barbero.Direccion, Barrio = usuario.Barbero.Barrio, FechaNacimiento = usuario.Barbero.FechaNacimiento,
                Especialidad = usuario.Barbero.Especialidad, FotoPerfil = usuario.FotoPerfil, Estado = usuario.Barbero.Estado,
                FechaContratacion = usuario.Barbero.FechaContratacion
            } : null
        };
    }

    private async Task<UsuarioDto> MapToDto(int usuarioId)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.Cliente)
            .Include(u => u.Barbero)
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario == null)
            throw new Exception("Usuario no encontrado");

        return MapUsuarioToDto(usuario);
    }

    public async Task<ServiceResult<object>> CreateAsync(UsuarioInput input)
    {
        if (input == null)
            return ServiceResult<object>.Fail("El objeto usuario es requerido");

        if (string.IsNullOrWhiteSpace(input.Nombre))
            return ServiceResult<object>.Fail("El nombre es requerido");

        if (string.IsNullOrWhiteSpace(input.Apellido))
            return ServiceResult<object>.Fail("El apellido es requerido");

        if (string.IsNullOrWhiteSpace(input.Correo))
            return ServiceResult<object>.Fail("El correo es requerido");

        if (string.IsNullOrWhiteSpace(input.Contrasena))
            return ServiceResult<object>.Fail("La contraseña es requerida");

        if (await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo))
            return ServiceResult<object>.Fail("Ya existe un usuario con ese correo");

        if (!string.IsNullOrWhiteSpace(input.Documento))
        {
            if (await _context.Usuarios.AnyAsync(u => u.Documento == input.Documento))
                return ServiceResult<object>.Fail("Ya existe un usuario con ese documento");
        }

        var rol = await _context.Roles.FindAsync(input.RolId);
        if (rol == null)
            return ServiceResult<object>.Fail("El rol especificado no existe");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (!BarberiaApi.Application.Helpers.ValidationHelper.ValidarUrlImagen(input.FotoPerfil, out var imgError))
            {
                return ServiceResult<object>.Fail(imgError!);
            }

            var usuario = new Usuario
            {
                Nombre = input.Nombre,
                Apellido = input.Apellido,
                Correo = input.Correo,
                Contrasena = input.Contrasena,
                RolId = input.RolId,
                TipoDocumento = input.TipoDocumento,
                Documento = input.Documento,
                FotoPerfil = input.FotoPerfil,
                Estado = input.Estado,
                FechaCreacion = DateTime.Now
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            if (input.RolId == 3)
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
            else if (input.RolId == 2)
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

            return ServiceResult<object>.Ok(await MapToDto(usuario.Id));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail($"Error interno: {ex.Message}", 500);
        }
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, UsuarioInput input)
    {
        if (input == null)
            return ServiceResult<object>.Fail("El objeto usuario es requerido");

        var usuarioExistente = await _context.Usuarios
            .Include(u => u.Cliente)
            .Include(u => u.Barbero)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuarioExistente == null)
            return ServiceResult<object>.NotFound();

        if (string.IsNullOrWhiteSpace(input.Nombre))
            return ServiceResult<object>.Fail("El nombre es requerido");

        if (string.IsNullOrWhiteSpace(input.Apellido))
            return ServiceResult<object>.Fail("El apellido es requerido");

        if (string.IsNullOrWhiteSpace(input.Correo))
            return ServiceResult<object>.Fail("El correo es requerido");

        if (input.Correo != usuarioExistente.Correo)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo && u.Id != id))
                return ServiceResult<object>.Fail("Ya existe otro usuario con ese correo");
        }

        if (!string.IsNullOrWhiteSpace(input.Documento) && input.Documento != usuarioExistente.Documento)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Documento == input.Documento && u.Id != id))
                return ServiceResult<object>.Fail("Ya existe otro usuario con ese documento");
        }

        if (input.RolId != usuarioExistente.RolId)
        {
            var rol = await _context.Roles.FindAsync(input.RolId);
            if (rol == null)
                return ServiceResult<object>.Fail("El rol especificado no existe");
        }

        if (!BarberiaApi.Application.Helpers.ValidationHelper.ValidarUrlImagen(input.FotoPerfil, out var imgErrorUpdate))
        {
            return ServiceResult<object>.Fail(imgErrorUpdate!);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            usuarioExistente.Nombre = input.Nombre;
            usuarioExistente.Apellido = input.Apellido;
            usuarioExistente.Correo = input.Correo;
            if (!string.IsNullOrWhiteSpace(input.Contrasena))
                usuarioExistente.Contrasena = input.Contrasena;
            
            var oldRolId = usuarioExistente.RolId;
            usuarioExistente.RolId = input.RolId;
            usuarioExistente.TipoDocumento = input.TipoDocumento;
            usuarioExistente.Documento = input.Documento;
            usuarioExistente.FotoPerfil = input.FotoPerfil;
            usuarioExistente.Estado = input.Estado;
            usuarioExistente.FechaModificacion = DateTime.Now;

            // Actualizar o Crear perfiles asociados según el rol
            if (input.RolId == 3) // Cliente
            {
                if (usuarioExistente.Cliente != null)
                {
                    usuarioExistente.Cliente.Telefono = input.Telefono;
                    usuarioExistente.Cliente.Direccion = input.Direccion;
                    usuarioExistente.Cliente.Barrio = input.Barrio;
                    usuarioExistente.Cliente.FechaNacimiento = input.FechaNacimiento;
                }
                else
                {
                    var nuevoCliente = new Cliente
                    {
                        UsuarioId = usuarioExistente.Id,
                        Telefono = input.Telefono,
                        Direccion = input.Direccion,
                        Barrio = input.Barrio,
                        FechaNacimiento = input.FechaNacimiento,
                        Estado = true,
                        FechaRegistro = DateTime.Now
                    };
                    _context.Clientes.Add(nuevoCliente);
                }
            }
            else if (input.RolId == 2) // Barbero
            {
                if (usuarioExistente.Barbero != null)
                {
                    usuarioExistente.Barbero.Telefono = input.Telefono;
                    usuarioExistente.Barbero.Direccion = input.Direccion;
                    usuarioExistente.Barbero.Barrio = input.Barrio;
                    usuarioExistente.Barbero.FechaNacimiento = input.FechaNacimiento;
                }
                else
                {
                    var nuevoBarbero = new Barbero
                    {
                        UsuarioId = usuarioExistente.Id,
                        Telefono = input.Telefono,
                        Direccion = input.Direccion,
                        Barrio = input.Barrio,
                        FechaNacimiento = input.FechaNacimiento,
                        Especialidad = "General",
                        Estado = true,
                        FechaContratacion = DateTime.Now
                    };
                    _context.Barberos.Add(nuevoBarbero);
                }
            }
            else
            {
                // Para otros roles (Admin, SuperAdmin), si existían perfiles previos, solo actualizamos los datos de contacto si el objeto no es nulo
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
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ServiceResult<object>.Ok(new { success = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            if (!await _context.Usuarios.AnyAsync(e => e.Id == id))
                return ServiceResult<object>.NotFound();
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail($"Error al actualizar: {ex.Message}", 500);
        }
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.Cliente)
            .Include(u => u.Barbero)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario == null) return ServiceResult<object>.NotFound();

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

        return ServiceResult<object>.Ok(response);
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Cliente)
            .Include(u => u.Barbero)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (usuario == null) return ServiceResult<object>.NotFound();
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
            return ServiceResult<object>.Ok(new { message = "Usuario eliminado permanentemente", eliminado = true, fisico = true });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServiceResult<object>.Fail($"Error interno: {ex.Message}", 500);
        }
    }
}
