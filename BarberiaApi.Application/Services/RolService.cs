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
        if (page < 1) page = 1; if (pageSize < 1) pageSize = 5;
        var baseQ = _context.Roles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(r => (r.Nombre != null && r.Nombre.ToLower().Contains(term)) || (r.Descripcion != null && r.Descripcion.ToLower().Contains(term)));
        }
        var totalCount = await baseQ.CountAsync();
        var items = await baseQ.OrderBy(r => r.Nombre).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new RoleDto { Id = r.Id, Nombre = r.Nombre, Descripcion = r.Descripcion, Estado = r.Estado ?? false,
                UsuariosAsignados = r.Usuarios.Count, Modulos = r.RolesModulos.Select(rm => rm.ModuloId).ToList() }).ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var rol = await _context.Roles.Include(r => r.RolesModulos).ThenInclude(rm => rm.Modulo).Include(r => r.Usuarios).FirstOrDefaultAsync(r => r.Id == id);
        if (rol == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(rol);
    }

    public async Task<ServiceResult<object>> CreateAsync(Role rol)
    {
        if (rol == null) return ServiceResult<object>.Fail("El rol es requerido");
        rol.Id = 0; rol.Estado = true;
        _context.Roles.Add(rol); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(rol);
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, RoleInput input)
    {
        if (id != input.Id) return ServiceResult<object>.Fail("Id no coincide");
        var rol = await _context.Roles.FindAsync(id);
        if (rol == null) return ServiceResult<object>.NotFound();
        rol.Nombre = input.Nombre; rol.Descripcion = input.Descripcion; rol.Estado = input.Estado;
        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Rol actualizado" });
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
