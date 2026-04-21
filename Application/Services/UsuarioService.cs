using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace BarberiaApi.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly BarberiaContext _context;
    private readonly IPhotoService _photoService;
    private readonly IMapper _mapper;

    public UsuarioService(BarberiaContext context, IPhotoService photoService, IMapper mapper)
    {
        _context = context;
        _photoService = photoService;
        _mapper = mapper;
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
        // NOTA: Validación estructural básica manejada por FluentValidation.

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

            var usuario = new Usuario
            {
                Nombre = input.Nombre,
                Apellido = input.Apellido,
                Correo = input.Correo,
                Contrasena = input.Contrasena, // En producción usar Hashing
                RolId = input.RolId,
                TipoDocumento = input.TipoDocumento,
                Documento = input.Documento,
                FotoPerfil = input.FotoPerfil,
                Estado = input.Estado,
                FechaCreacion = DateTime.Now
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

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
        var usuarioExistente = await _context.Usuarios
            .Include(u => u.Cliente)
            .Include(u => u.Barbero)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuarioExistente == null) return ServiceResult<object>.NotFound();

        // NOTA: Datos estructurales validados por FluentValidation.

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
                usuarioExistente.Contrasena = input.Contrasena;
            
            usuarioExistente.RolId = input.RolId;
            usuarioExistente.TipoDocumento = input.TipoDocumento;
            usuarioExistente.Documento = input.Documento;
            usuarioExistente.FotoPerfil = input.FotoPerfil;
            usuarioExistente.Estado = input.Estado;
            usuarioExistente.FechaModificacion = DateTime.Now;

            if (input.RolId == 3) // Cliente
            {
                if (usuarioExistente.Cliente != null)
                {
                    usuarioExistente.Cliente.Telefono = input.Telefono;
                    usuarioExistente.Cliente.Direccion = input.Direccion;
                    usuarioExistente.Cliente.Barrio = input.Barrio;
                    usuarioExistente.Cliente.FechaNacimiento = input.FechaNacimiento;
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
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ServiceResult<object>.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail($"Error al actualizar: {ex.Message}", 500);
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

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Usuario eliminado permanentemente", eliminado = true });
    }
}
