using BarberiaApi.Application.Common;
using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Helpers;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Constants;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using BCrypt.Net;

namespace BarberiaApi.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly BarberiaContext _context;
    private readonly IPhotoService _photoService;
    private readonly IMapper _mapper;
    private readonly IDateTimeProvider _dt;
    private readonly IFirebaseAuthService _firebaseAuth;

    public UsuarioService(BarberiaContext context, IPhotoService photoService, IMapper mapper, IDateTimeProvider dt, IFirebaseAuthService firebaseAuth)
    {
        _context = context;
        _photoService = photoService;
        _mapper = mapper;
        _dt = dt;
        _firebaseAuth = firebaseAuth;
    }

    public async Task<ServiceResult<object>> AnalisisAsync(int page, int pageSize)
    {
        PaginationHelper.Sanitize(ref page, ref pageSize);

        var q = _context.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Nombre).ThenBy(u => u.Apellido)
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
                EntregasRegistradas = 0,
                VentasComoCliente = u.Cliente != null ? u.Cliente.Venta.Count() : 0,
                AgendamientosCliente = u.Cliente != null ? u.Cliente.Agendamientos.Count() : 0,
                DevolucionesCliente = u.Cliente != null ? u.Cliente.Devoluciones.Count() : 0,
                AgendamientosBarbero = u.Barbero != null ? u.Barbero.Agendamientos.Count() : 0,
                EntregasBarbero = 0,
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
                EntregasRegistradas = 0,
                VentasComoCliente = u.Cliente != null ? u.Cliente.Venta.Count() : 0,
                AgendamientosCliente = u.Cliente != null ? u.Cliente.Agendamientos.Count() : 0,
                DevolucionesCliente = u.Cliente != null ? u.Cliente.Devoluciones.Count() : 0,
                AgendamientosBarbero = u.Barbero != null ? u.Barbero.Agendamientos.Count() : 0,
                EntregasBarbero = 0,
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
        PaginationHelper.Sanitize(ref page, ref pageSize);

        var baseQ = _context.Usuarios.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(u =>
                (u.Nombre != null && u.Nombre.ToLower().Contains(term)) ||
                (u.Apellido != null && u.Apellido.ToLower().Contains(term)) ||
                (u.Documento != null && u.Documento.ToLower().Contains(term)) ||
                (u.Correo != null && u.Correo.ToLower().Contains(term))
            );
        }

        var totalCount = await baseQ.CountAsync();
        var items = await baseQ
            .OrderBy(u => u.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<UsuarioDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == id)
            .ProjectTo<UsuarioDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        if (usuario == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(usuario);
    }

    private async Task<UsuarioDto> MapToDto(int usuarioId)
    {
        var dto = await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .ProjectTo<UsuarioDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        if (dto == null) throw new Exception("Usuario no encontrado");
        return dto;
    }

    public async Task<ServiceResult<object>> CreateAsync(UsuarioInput input)
    {
        // Validación de solo letras y espacios para Nombre y Apellido
        if (!ValidationHelper.ValidarSoloLetras(input.Nombre, out string? errorNombre))
            return ServiceResult<object>.Fail($"Nombre: {errorNombre}");
        
        if (!ValidationHelper.ValidarSoloLetras(input.Apellido, out string? errorApellido))
            return ServiceResult<object>.Fail($"Apellido: {errorApellido}");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo))
                return ServiceResult<object>.Fail("Ya existe un usuario con ese correo");

            if (!string.IsNullOrWhiteSpace(input.Documento))
            {
                if (await _context.Usuarios.AnyAsync(u => u.Documento == input.Documento))
                    return ServiceResult<object>.Fail("Ya existe un usuario con ese documento");
            }

            var contrasenaHasheada = string.IsNullOrWhiteSpace(input.Contrasena)
                ? BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                : BCrypt.Net.BCrypt.HashPassword(input.Contrasena);

            var usuario = new Usuario
            {
                Nombre = input.Nombre,
                Apellido = input.Apellido,
                Correo = input.Correo,
                Contrasena = contrasenaHasheada,
                RolId = input.RolId,
                TipoDocumento = input.TipoDocumento,
                Documento = input.Documento,
                FotoPerfil = input.FotoPerfil,
                Estado = input.Estado,
                FechaCreacion = _dt.NowColombia
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Resolver el nombre del rol para evitar comparaciones por ID numérico
            var nombreRol = input.RolId > 0
                ? (await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == input.RolId))?.Nombre ?? string.Empty
                : string.Empty;

            if (string.Equals(nombreRol, RolesNombres.Cliente, StringComparison.OrdinalIgnoreCase))
            {
                var cliente = new Cliente
                {
                    UsuarioId = usuario.Id,
                    Telefono = input.Telefono,
                    Direccion = input.Direccion,
                    Barrio = input.Barrio,
                    FechaNacimiento = input.FechaNacimiento,
                    Estado = true,
                    FechaRegistro = _dt.NowColombia
                };
                _context.Clientes.Add(cliente);
            }
            else if (string.Equals(nombreRol, RolesNombres.Barbero, StringComparison.OrdinalIgnoreCase))
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
                    FechaContratacion = _dt.NowColombia
                };
                _context.Barberos.Add(barbero);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<object>.Ok(await MapToDto(usuario.Id));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail("Error al crear el usuario.", 500);
        }
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, UsuarioInput input)
    {
        var usuarioExistente = await _context.Usuarios
            .Include(u => u.Cliente)
            .Include(u => u.Barbero)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuarioExistente == null) return ServiceResult<object>.NotFound();

        // Validación de solo letras y espacios para Nombre y Apellido
        if (!ValidationHelper.ValidarSoloLetras(input.Nombre, out string? errorNombre))
            return ServiceResult<object>.Fail($"Nombre: {errorNombre}");
        
        if (!ValidationHelper.ValidarSoloLetras(input.Apellido, out string? errorApellido))
            return ServiceResult<object>.Fail($"Apellido: {errorApellido}");

        if (input.Correo != usuarioExistente.Correo)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo && u.Id != id))
                return ServiceResult<object>.Fail("Ya existe otro usuario con ese correo");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            usuarioExistente.Nombre = input.Nombre;
            usuarioExistente.Apellido = input.Apellido;
            usuarioExistente.Correo = input.Correo;
            if (!string.IsNullOrWhiteSpace(input.Contrasena))
                usuarioExistente.Contrasena = BCrypt.Net.BCrypt.HashPassword(input.Contrasena);
            
            usuarioExistente.RolId = input.RolId;
            usuarioExistente.TipoDocumento = input.TipoDocumento;
            usuarioExistente.Documento = input.Documento;
            usuarioExistente.FotoPerfil = input.FotoPerfil;
            usuarioExistente.Estado = input.Estado;
            usuarioExistente.FechaModificacion = _dt.NowColombia;

            // Resolver el nombre del rol para evitar comparaciones por ID numérico
            var nombreRolUpdate = input.RolId > 0
                ? (await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == input.RolId))?.Nombre ?? string.Empty
                : string.Empty;

            if (string.Equals(nombreRolUpdate, RolesNombres.Cliente, StringComparison.OrdinalIgnoreCase))
            {
                if (usuarioExistente.Cliente != null)
                {
                    usuarioExistente.Cliente.Telefono = input.Telefono;
                    usuarioExistente.Cliente.Direccion = input.Direccion;
                    usuarioExistente.Cliente.Barrio = input.Barrio;
                    usuarioExistente.Cliente.FechaNacimiento = input.FechaNacimiento;
                }
            }
            else if (string.Equals(nombreRolUpdate, RolesNombres.Barbero, StringComparison.OrdinalIgnoreCase))
            {
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
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail("Error al actualizar el usuario.", 500);
        }
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return ServiceResult<object>.NotFound();

        usuario.Estado = input.estado;
        await _context.SaveChangesAsync();

        return ServiceResult<object>.Ok(new { success = true, mensaje = "Estado actualizado" });
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Cliente)
            .Include(u => u.Barbero)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (usuario == null) return ServiceResult<object>.NotFound();

        // Capturamos el correo ANTES de anonimizar, para liberarlo también en Firebase.
        var correoFirebase = usuario.Correo;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Liberamos el correo en Firebase Authentication (idempotente: no falla si no existe).
            await _firebaseAuth.DeleteUserByEmailAsync(correoFirebase);

            return ServiceResult<object>.Ok(new { message = "Usuario eliminado permanentemente", anonimizado = false });
        }
        catch (DbUpdateException)
        {
            // FK constraint — anonimizar datos personales y liberar correo/documento
            await transaction.RollbackAsync();

            // El Remove() dejó la entidad en estado Deleted en el change tracker.
            // Debemos resetearla a Modified para que EF genere un UPDATE, no otro DELETE.
            _context.Entry(usuario).State = EntityState.Modified;

            usuario.Nombre = "Usuario";
            usuario.Apellido = "Eliminado";
            usuario.Correo = $"eliminado_{id}@baja.local";
            usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());
            usuario.Documento = null;
            usuario.TipoDocumento = null;
            usuario.FotoPerfil = null;
            usuario.Estado = false;
            usuario.FechaModificacion = _dt.NowColombia;

            if (usuario.Cliente != null)
            {
                _context.Entry(usuario.Cliente).State = EntityState.Modified;
                usuario.Cliente.Telefono = null;
                usuario.Cliente.Direccion = null;
                usuario.Cliente.Barrio = null;
                usuario.Cliente.FechaNacimiento = null;
                usuario.Cliente.Estado = false;
            }

            if (usuario.Barbero != null)
            {
                _context.Entry(usuario.Barbero).State = EntityState.Modified;
                usuario.Barbero.Telefono = null;
                usuario.Barbero.Direccion = null;
                usuario.Barbero.Barrio = null;
                usuario.Barbero.FechaNacimiento = null;
                usuario.Barbero.Estado = false;
            }

            await _context.SaveChangesAsync();

            // Aunque en SQL solo anonimizamos (hay historial con FK), el correo original
            // ya quedó liberado, así que también lo borramos de Firebase para poder reutilizarlo.
            await _firebaseAuth.DeleteUserByEmailAsync(correoFirebase);

            return ServiceResult<object>.Ok(new { message = "Datos personales eliminados, historial conservado", anonimizado = true });
        }
    }
}
