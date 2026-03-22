using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class ModuloService : IModuloService
{
    private readonly BarberiaContext _context;
    public ModuloService(BarberiaContext context) => _context = context;

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1; if (pageSize < 1) pageSize = 5;
        var baseQ = _context.Modulos.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(m => m.Nombre != null && m.Nombre.ToLower().Contains(term));
        }
        var totalCount = await baseQ.CountAsync();
        var items = await baseQ.OrderBy(m => m.Nombre).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var modulo = await _context.Modulos.FindAsync(id);
        if (modulo == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(modulo);
    }

    public async Task<ServiceResult<object>> CreateAsync(Modulos modulo)
    {
        modulo.Id = 0; modulo.Estado = true;
        _context.Modulos.Add(modulo); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(modulo);
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, Modulos modulo)
    {
        if (id != modulo.Id) return ServiceResult<object>.Fail("Id no coincide");
        _context.Entry(modulo).State = EntityState.Modified;
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { if (!await _context.Modulos.AnyAsync(m => m.Id == id)) return ServiceResult<object>.NotFound(); throw; }
        return ServiceResult<object>.Ok(new { message = "Módulo actualizado" });
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var modulo = await _context.Modulos.Include(m => m.RolesModulos).ThenInclude(rm => rm.Rol).FirstOrDefaultAsync(m => m.Id == id);
        if (modulo == null) return ServiceResult<object>.NotFound();
        bool tieneRolesActivos = modulo.RolesModulos.Any(rm => rm.Rol != null && rm.Rol.Estado == true);
        if (tieneRolesActivos)
        {
            modulo.Estado = false; await _context.SaveChangesAsync();
            return ServiceResult<object>.Ok(new { message = "Módulo desactivado (borrado lógico por estar asignado a roles activos)", eliminado = true, fisico = false,
                rolesAsociados = modulo.RolesModulos.Count(rm => rm.Rol != null && rm.Rol.Estado == true) });
        }
        _context.Modulos.Remove(modulo); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Módulo eliminado físicamente de la base de datos", eliminado = true, fisico = true });
    }
}
