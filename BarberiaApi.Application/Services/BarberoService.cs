using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class BarberoService : IBarberoService
{
    private readonly BarberiaContext _context;
    public BarberoService(BarberiaContext context) => _context = context;

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;
        var baseQ = _context.Barberos.Include(b => b.Usuario).ThenInclude(u => u.Rol).AsNoTracking().AsQueryable();
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
            .Select(b => MapToDto(b)).ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var barbero = await _context.Barberos.AsNoTracking().Include(b => b.Usuario).ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (barbero == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(MapToDto(barbero));
    }

    public async Task<ServiceResult<object>> CreateAsync(BarberoInput input)
    {
        if (input == null) return ServiceResult<object>.Fail("El objeto barbero es requerido");
        if (string.IsNullOrWhiteSpace(input.Nombre)) return ServiceResult<object>.Fail("El nombre es requerido");
        if (string.IsNullOrWhiteSpace(input.Apellido)) return ServiceResult<object>.Fail("El apellido es requerido");
        if (string.IsNullOrWhiteSpace(input.Documento)) return ServiceResult<object>.Fail("El documento es requerido");
        if (string.IsNullOrWhiteSpace(input.Correo)) return ServiceResult<object>.Fail("El correo es requerido");

        var usuario = await _context.Usuarios.FindAsync(input.UsuarioId);
        if (usuario == null) return ServiceResult<object>.Fail("El usuario no existe");
        if (usuario.RolId != 2) return ServiceResult<object>.Fail("El usuario no tiene un rol de Barbero");
        if (await _context.Barberos.AnyAsync(b => b.UsuarioId == input.UsuarioId))
            return ServiceResult<object>.Fail("Ya existe un perfil de barbero para este usuario");
        if (await _context.Usuarios.AnyAsync(u => u.Documento == input.Documento && u.Id != input.UsuarioId))
            return ServiceResult<object>.Fail("El documento ya está registrado en otro usuario");
        if (!string.IsNullOrWhiteSpace(input.Correo) && await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo && u.Id != input.UsuarioId))
            return ServiceResult<object>.Fail("Ya existe otro usuario con ese correo");

        var barbero = new Barbero
        {
            UsuarioId = input.UsuarioId, Telefono = input.Telefono, Direccion = input.Direccion,
            Barrio = input.Barrio, FechaNacimiento = input.FechaNacimiento,
            Especialidad = input.Especialidad ?? "General", Estado = input.Estado, FechaContratacion = DateTime.Now
        };
        _context.Barberos.Add(barbero);
        await _context.SaveChangesAsync();

        var barberoCreado = await _context.Barberos.Include(b => b.Usuario).ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(b => b.Id == barbero.Id);
        return ServiceResult<object>.Ok(MapToDto(barberoCreado!));
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, BarberoInput input)
    {
        if (input == null) return ServiceResult<object>.Fail("El objeto barbero es requerido");
        var barberoExistente = await _context.Barberos.Include(b => b.Usuario).FirstOrDefaultAsync(b => b.Id == id);
        if (barberoExistente == null) return ServiceResult<object>.NotFound();
        if (string.IsNullOrWhiteSpace(input.Nombre)) return ServiceResult<object>.Fail("El nombre es requerido");
        if (string.IsNullOrWhiteSpace(input.Apellido)) return ServiceResult<object>.Fail("El apellido es requerido");
        if (string.IsNullOrWhiteSpace(input.Documento)) return ServiceResult<object>.Fail("El documento es requerido");
        if (string.IsNullOrWhiteSpace(input.Correo)) return ServiceResult<object>.Fail("El correo es requerido");

        if (input.Documento != barberoExistente.Usuario.Documento)
            if (await _context.Usuarios.AnyAsync(u => u.Documento == input.Documento && u.Id != barberoExistente.UsuarioId))
                return ServiceResult<object>.Fail("Ya existe otro barbero con ese documento");
        if (input.Correo != barberoExistente.Usuario.Correo)
            if (await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo && u.Id != barberoExistente.UsuarioId))
                return ServiceResult<object>.Fail("Ya existe otro barbero con ese correo");

        barberoExistente.Telefono = input.Telefono; barberoExistente.Direccion = input.Direccion;
        barberoExistente.Barrio = input.Barrio; barberoExistente.FechaNacimiento = input.FechaNacimiento;
        barberoExistente.Especialidad = input.Especialidad ?? "General"; barberoExistente.Estado = input.Estado;

        var usuario = await _context.Usuarios.FindAsync(barberoExistente.UsuarioId);
        if (usuario != null)
        {
            if (!Helpers.ValidationHelper.ValidarUrlImagen(input.FotoPerfil, out var imgError))
                return ServiceResult<object>.Fail(imgError!);
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
        return ServiceResult<object>.Ok(new CambioEstadoResponse<BarberoDto>
        {
            entidad = MapToDto(barbero),
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
                message = "Barbero y usuario desactivados (historial asociado)", eliminado = true, fisico = false,
                motivos = new {
                    agendamientosActivos = barbero.Agendamientos.Count(a => a.Estado != "Cancelada"),
                    entregasAsociadas = barbero.EntregasInsumos.Count(), ventasComoBarbero = tieneVentasComoBarbero,
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
            return ServiceResult<object>.Ok(new { message = "Usuario y barbero eliminados físicamente", eliminado = true, fisico = true });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServiceResult<object>.Fail($"Error interno: {ex.Message}", 500);
        }
    }

    private static BarberoDto MapToDto(Barbero b) => new()
    {
        Id = b.Id, UsuarioId = b.UsuarioId, Nombre = b.Usuario?.Nombre, Apellido = b.Usuario?.Apellido,
        Documento = b.Usuario?.Documento ?? "", Correo = b.Usuario?.Correo, Telefono = b.Telefono,
        Direccion = b.Direccion, Barrio = b.Barrio, FechaNacimiento = b.FechaNacimiento,
        Especialidad = b.Especialidad, FotoPerfil = b.Usuario?.FotoPerfil, Estado = b.Estado,
        FechaContratacion = b.FechaContratacion,
        Usuario = b.Usuario == null ? null : new UsuarioDto
        {
            Id = b.Usuario.Id, Nombre = b.Usuario.Nombre, Apellido = b.Usuario.Apellido,
            Correo = b.Usuario.Correo, RolId = b.Usuario.RolId, RolNombre = b.Usuario.Rol?.Nombre,
            Estado = b.Usuario.Estado, FechaCreacion = b.Usuario.FechaCreacion
        }
    };
}
