using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace BarberiaApi.Application.Services;

public class BarberoService : IBarberoService
{
    private readonly BarberiaContext _context;
    private readonly IMapper _mapper;

    public BarberoService(BarberiaContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;
        var baseQ = _context.Barberos.AsNoTracking().AsQueryable();
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
        var items = await baseQ.OrderBy(b => b.Usuario.Nombre).Skip((page - 1) * pageSize).Take(pageSize)
            .ProjectTo<BarberoDto>(_mapper.ConfigurationProvider).ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var barbero = await _context.Barberos.AsNoTracking()
            .ProjectTo<BarberoDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (barbero == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(barbero);
    }

    public async Task<ServiceResult<object>> CreateAsync(BarberoInput input)
    {
        // NOTA: Validación estructural básica manejada por FluentValidation.

        var usuario = await _context.Usuarios.FindAsync(input.UsuarioId);
        if (usuario == null) return ServiceResult<object>.Fail("El usuario no existe");
        if (usuario.RolId != 2) return ServiceResult<object>.Fail("El usuario no tiene un rol de Barbero");
        if (await _context.Barberos.AnyAsync(b => b.UsuarioId == input.UsuarioId))
            return ServiceResult<object>.Fail("Ya existe un perfil de barbero para este usuario");
        
        var barbero = new Barbero
        {
            UsuarioId = input.UsuarioId, Telefono = input.Telefono, Direccion = input.Direccion,
            Barrio = input.Barrio, FechaNacimiento = input.FechaNacimiento,
            Especialidad = input.Especialidad ?? "General", Estado = input.Estado, FechaContratacion = DateTime.Now
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

        var usuario = await _context.Usuarios.FindAsync(barberoExistente.UsuarioId);
        if (usuario != null)
        {
            usuario.Nombre = input.Nombre; usuario.Apellido = input.Apellido;
            usuario.Documento = input.Documento; usuario.Correo = input.Correo; usuario.FotoPerfil = input.FotoPerfil;
        }
        await _context.SaveChangesAsync();
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
        var barbero = await _context.Barberos.Include(b => b.Agendamientos).Include(b => b.EntregasInsumos).Include(b => b.Usuario)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (barbero == null) return ServiceResult<object>.NotFound();
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
