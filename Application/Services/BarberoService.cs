using BarberiaApi.Application.Common;
using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Constants;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace BarberiaApi.Application.Services;

public class BarberoService : IBarberoService
{
    private readonly BarberiaContext _context;
    private readonly IMapper _mapper;
    private readonly IDateTimeProvider _dt;
    private readonly IFirebaseAuthService _firebaseAuth;

    public BarberoService(BarberiaContext context, IMapper mapper, IDateTimeProvider dt, IFirebaseAuthService firebaseAuth)
    {
        _context = context;
        _mapper = mapper;
        _dt = dt;
        _firebaseAuth = firebaseAuth;
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        PaginationHelper.Sanitize(ref page, ref pageSize);
        // Solo incluir barberos cuyo usuario tiene el rol "barbero" (previene perfiles huérfanos por cambio de rol)
        var barberoRoleId = await _context.Roles
            .Where(r => r.Nombre.ToLower() == RolesNombres.Barbero.ToLower())
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync();
        var baseQ = _context.Barberos.AsNoTracking()
            .Where(b => b.Usuario != null && b.Usuario.RolId == barberoRoleId)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(b =>
                (b.Usuario != null && ((b.Usuario.Nombre != null && b.Usuario.Nombre.ToLower().Contains(term)) ||
                (b.Usuario.Apellido != null && b.Usuario.Apellido.ToLower().Contains(term)) ||
                (b.Usuario.Documento != null && b.Usuario.Documento.ToLower().Contains(term)) ||
                (b.Usuario.Correo != null && b.Usuario.Correo.ToLower().Contains(term)))) ||
                (b.Telefono != null && b.Telefono.ToLower().Contains(term)) ||
                (b.Direccion != null && b.Direccion.ToLower().Contains(term)) ||
                (b.Barrio != null && b.Barrio.ToLower().Contains(term)) ||
                (b.Especialidad != null && b.Especialidad.ToLower().Contains(term)));
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
                Nombre = b.Usuario != null ? b.Usuario.Nombre ?? string.Empty : string.Empty,
                Apellido = b.Usuario != null ? b.Usuario.Apellido ?? string.Empty : string.Empty,
                Documento = b.Usuario != null ? b.Usuario.Documento ?? string.Empty : string.Empty,
                Correo = b.Usuario != null ? b.Usuario.Correo ?? string.Empty : string.Empty,
                Telefono = b.Telefono,
                Direccion = b.Direccion,
                Barrio = b.Barrio,
                FechaNacimiento = b.FechaNacimiento,
                Especialidad = b.Especialidad ?? string.Empty,
                FotoPerfil = b.Usuario != null ? b.Usuario.FotoPerfil : null,
                Estado = b.Estado,
                FechaContratacion = b.FechaContratacion,
                SaldoDisponible = b.CreditosBarbero != null && b.CreditosBarbero.Any()
                    ? b.CreditosBarbero
                        .OrderByDescending(c => c.FechaInicio)
                        .Select(c => c.LimiteCredito - c.SaldoPendiente)
                        .FirstOrDefault()
                    : 200000m
            })
            .ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var barbero = await _context.Barberos.AsNoTracking()
            .ProjectTo<BarberoDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (barbero == null) return ServiceResult<object>.NotFound();

        // SaldoDisponible no lo calcula AutoMapper (Ignore) — calcularlo igual que GetAll
        var saldo = await _context.CreditosBarbero
            .Where(c => c.BarberoId == id)
            .OrderByDescending(c => c.FechaInicio)
            .Select(c => (decimal?)(c.LimiteCredito - c.SaldoPendiente))
            .FirstOrDefaultAsync();
        barbero.SaldoDisponible = saldo ?? 200000m;

        return ServiceResult<object>.Ok(barbero);
    }

    public async Task<ServiceResult<object>> CreateAsync(BarberoInput input)
    {
        // NOTA: Validación estructural básica manejada por FluentValidation.

        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == input.UsuarioId);
        if (usuario == null) return ServiceResult<object>.Fail("El usuario no existe");
        if (!string.Equals(usuario.Rol?.Nombre, RolesNombres.Barbero, StringComparison.OrdinalIgnoreCase))
            return ServiceResult<object>.Fail("El usuario no tiene un rol de Barbero");
        if (await _context.Barberos.AnyAsync(b => b.UsuarioId == input.UsuarioId))
            return ServiceResult<object>.Fail("Ya existe un perfil de barbero para este usuario");
        
        var barbero = new Barbero
        {
            UsuarioId = input.UsuarioId, Telefono = input.Telefono, Direccion = input.Direccion,
            Barrio = input.Barrio, FechaNacimiento = input.FechaNacimiento,
            Especialidad = input.Especialidad ?? "General", Estado = input.Estado, FechaContratacion = _dt.NowColombia
        };
        _context.Barberos.Add(barbero);
        await _context.SaveChangesAsync();

        var dto = await _context.Barberos.AsNoTracking()
            .ProjectTo<BarberoDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(b => b.Id == barbero.Id);

        return ServiceResult<object>.Ok(dto!);
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, BarberoInput input)
    {
        var barberoExistente = await _context.Barberos.Include(b => b.Usuario).FirstOrDefaultAsync(b => b.Id == id);
        if (barberoExistente == null) return ServiceResult<object>.NotFound();
        
        // NOTA: Datos estructurales validados por FluentValidation.

        barberoExistente.Telefono = input.Telefono; barberoExistente.Direccion = input.Direccion;
        barberoExistente.Barrio = input.Barrio; barberoExistente.FechaNacimiento = input.FechaNacimiento;
        barberoExistente.Especialidad = input.Especialidad ?? "General"; barberoExistente.Estado = input.Estado;

        var usuario = barberoExistente.Usuario;
        bool correoCambio = false;
        string correoAntiguo = string.Empty;
        if (usuario != null)
        {
            correoCambio = usuario.Correo != input.Correo;
            correoAntiguo = usuario.Correo;
            usuario.Nombre = input.Nombre; usuario.Apellido = input.Apellido;
            usuario.Documento = input.Documento; usuario.Correo = input.Correo;
            if (input.FotoPerfil != null)
                usuario.FotoPerfil = input.FotoPerfil;
        }
        await _context.SaveChangesAsync();

        // Si el correo cambió, actualizar en Firebase Authentication
        if (correoCambio)
        {
            await _firebaseAuth.UpdateUserEmailAsync(correoAntiguo, input.Correo);
        }

        return ServiceResult<object>.Ok(new { message = "Barbero actualizado" });
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input)
    {
        var barbero = await _context.Barberos.Include(b => b.Usuario).FirstOrDefaultAsync(b => b.Id == id);
        if (barbero == null) return ServiceResult<object>.NotFound();
        barbero.Estado = input.estado;
        if (barbero.Usuario != null) barbero.Usuario.Estado = input.estado;
        await _context.SaveChangesAsync();

        var dto = await _context.Barberos.AsNoTracking()
            .ProjectTo<BarberoDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(b => b.Id == id);

        return ServiceResult<object>.Ok(new CambioEstadoResponse<BarberoDto>
        {
            entidad = dto,
            mensaje = input.estado ? "Barbero activado exitosamente" : "Barbero desactivado exitosamente", exitoso = true
        });
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var barbero = await _context.Barberos.Include(b => b.Agendamientos).Include(b => b.Usuario)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (barbero == null) return ServiceResult<object>.NotFound();
        var usuario = barbero.Usuario;
        bool tieneAgendamientosActivos = barbero.Agendamientos.Any(a => a.Estado != "Cancelada");
        bool tieneVentasComoBarbero = await _context.Ventas.AnyAsync(v => v.BarberoId == barbero.Id);
        bool tieneRegistroUsuario = usuario != null && (
            await _context.Compras.AnyAsync(c => c.UsuarioId == usuario.Id)
            || await _context.Devoluciones.AnyAsync(d => d.UsuarioId == usuario.Id)
            || await _context.Ventas.AnyAsync(v => v.UsuarioId == usuario.Id));

        if (tieneAgendamientosActivos || tieneVentasComoBarbero || tieneRegistroUsuario)
        {
            barbero.Estado = false;
            if (usuario != null) usuario.Estado = false;
            await _context.SaveChangesAsync();
            return ServiceResult<object>.Ok(new {
                message = "Barbero y usuario desactivados (historial asociado)", eliminado = true, fisico = false
            });
        }
        _context.Barberos.Remove(barbero);
        if (usuario != null) _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Usuario y barbero eliminados físicamente", eliminado = true, fisico = true });
    }
}
