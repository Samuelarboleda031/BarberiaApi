using BarberiaApi.Application.Common;
using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class RolService : IRolService
{
    private readonly BarberiaContext _context;
    public RolService(BarberiaContext context) => _context = context;

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        PaginationHelper.Sanitize(ref page, ref pageSize);
        var baseQ = _context.Roles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(r => (r.Nombre != null && r.Nombre.ToLower().Contains(term)) || (r.Descripcion != null && r.Descripcion.ToLower().Contains(term)));
        }
        var totalCount = await baseQ.CountAsync();
        var items = await baseQ.OrderBy(r => r.Nombre).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new RoleDto {
                Id = r.Id,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                Estado = r.Estado ?? false,
                UsuariosAsignados = r.Usuarios.Count(),
                Modulos = r.RolesModulos.Select(rm => rm.ModuloId).ToList()
            }).ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var rol = await _context.Roles.Include(r => r.RolesModulos).ThenInclude(rm => rm.Modulo).Include(r => r.Usuarios).FirstOrDefaultAsync(r => r.Id == id);
        if (rol == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(rol);
    }

    public async Task<ServiceResult<object>> CreateAsync(CreateRoleInput input)
    {
        if (input == null) return ServiceResult<object>.Fail("El rol es requerido");
        var rol = new Role { Nombre = input.Nombre, Descripcion = input.Descripcion, Estado = true };
        _context.Roles.Add(rol);
        await _context.SaveChangesAsync();

        foreach (var moduloId in input.Modulos)
        {
            input.Permisos.TryGetValue(moduloId.ToString(), out var p);
            _context.RolesModulos.Add(new RolesModulos
            {
                RolId = rol.Id,
                ModuloId = moduloId,
                PuedeVer = p?.PuedeVer ?? true,
                PuedeCrear = p?.PuedeCrear ?? true,
                PuedeEditar = p?.PuedeEditar ?? true,
                PuedeEliminar = p?.PuedeEliminar ?? true
            });
        }
        await _context.SaveChangesAsync();

        return ServiceResult<object>.Ok(new RoleDto
        {
            Id = rol.Id,
            Nombre = rol.Nombre,
            Descripcion = rol.Descripcion,
            Estado = rol.Estado ?? false,
            UsuariosAsignados = 0,
            Modulos = input.Modulos
        });
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, RoleInput input)
    {
        if (id != input.Id) return ServiceResult<object>.Fail("Id no coincide");
        var rol = await _context.Roles
            .Include(r => r.RolesModulos)
            .Include(r => r.Usuarios)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (rol == null) return ServiceResult<object>.NotFound();

        rol.Nombre = input.Nombre;
        rol.Descripcion = input.Descripcion;
        rol.Estado = input.Estado;

        // Sincronizar módulos
        var existentes = rol.RolesModulos.ToList();
        var aEliminar = existentes.Where(rm => !input.Modulos.Contains(rm.ModuloId)).ToList();
        _context.RolesModulos.RemoveRange(aEliminar);

        foreach (var moduloId in input.Modulos)
        {
            input.Permisos.TryGetValue(moduloId.ToString(), out var p);
            var existente = existentes.FirstOrDefault(rm => rm.ModuloId == moduloId);
            if (existente != null)
            {
                if (p != null)
                {
                    existente.PuedeVer = p.PuedeVer;
                    existente.PuedeCrear = p.PuedeCrear;
                    existente.PuedeEditar = p.PuedeEditar;
                    existente.PuedeEliminar = p.PuedeEliminar;
                }
            }
            else
            {
                _context.RolesModulos.Add(new RolesModulos
                {
                    RolId = id,
                    ModuloId = moduloId,
                    PuedeVer = p?.PuedeVer ?? true,
                    PuedeCrear = p?.PuedeCrear ?? true,
                    PuedeEditar = p?.PuedeEditar ?? true,
                    PuedeEliminar = p?.PuedeEliminar ?? true
                });
            }
        }

        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new RoleDto
        {
            Id = rol.Id,
            Nombre = rol.Nombre,
            Descripcion = rol.Descripcion,
            Estado = rol.Estado ?? false,
            UsuariosAsignados = rol.Usuarios.Count(),
            Modulos = input.Modulos
        });
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input)
    {
        var rol = await _context.Roles.Include(r => r.Usuarios).FirstOrDefaultAsync(r => r.Id == id);
        if (rol == null) return ServiceResult<object>.NotFound();
        if (!input.estado && rol.Usuarios.Any())
            return ServiceResult<object>.Fail("No se puede desactivar el rol porque tiene usuarios asociados", 409);
        rol.Estado = input.estado; await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new CambioEstadoResponse<Role> { entidad = rol,
            mensaje = input.estado ? "Rol activado exitosamente" : "Rol desactivado exitosamente", exitoso = true });
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var rol = await _context.Roles.Include(r => r.Usuarios).Include(r => r.RolesModulos).FirstOrDefaultAsync(r => r.Id == id);
        if (rol == null) return ServiceResult<object>.NotFound();
        if (rol.Usuarios.Any())
            return ServiceResult<object>.Fail("No se puede eliminar el rol porque tiene usuarios asociados", 409);
        _context.Roles.Remove(rol); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Rol eliminado físicamente de la base de datos", eliminado = true, fisico = true, modulosEliminados = true });
    }
}
