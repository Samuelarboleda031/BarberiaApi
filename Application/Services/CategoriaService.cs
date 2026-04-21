using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace BarberiaApi.Application.Services;

public class CategoriaService : ICategoriaService
{
    private readonly BarberiaContext _context;
    private readonly IMapper _mapper;

    public CategoriaService(BarberiaContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1; if (pageSize < 1) pageSize = 5;
        var baseQ = _context.Categorias.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(c => c.Nombre != null && c.Nombre.ToLower().Contains(term));
        }
        var totalCount = await baseQ.CountAsync();
        var items = await baseQ.OrderBy(c => c.Nombre).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var categoria = await _context.Categorias.Include(c => c.Productos).FirstOrDefaultAsync(c => c.Id == id);
        if (categoria == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(categoria);
    }

    public async Task<ServiceResult<object>> CreateAsync(Categoria categoria)
    {
        // NOTA: Validación estructural básica manejada por FluentValidation.
        categoria.Id = 0; categoria.Estado = true;
        _context.Categorias.Add(categoria); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(categoria);
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, Categoria categoria)
    {
        if (id != categoria.Id) return ServiceResult<object>.Fail("Id no coincide");
        var existing = await _context.Categorias.FindAsync(id);
        if (existing == null) return ServiceResult<object>.NotFound();
        if (!string.IsNullOrWhiteSpace(categoria.Nombre) && categoria.Nombre != existing.Nombre)
            if (await _context.Categorias.AnyAsync(c => c.Nombre == categoria.Nombre && c.Id != id && c.Estado == true))
                return ServiceResult<object>.Fail("Ya existe otra categoría con ese nombre");
        _context.Entry(existing).CurrentValues.SetValues(categoria);
        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Categoría actualizada" });
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input)
    {
        var categoria = await _context.Categorias.Include(c => c.Productos).FirstOrDefaultAsync(c => c.Id == id);
        if (categoria == null) return ServiceResult<object>.NotFound();
        categoria.Estado = input.estado; await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new CambioEstadoResponse<Categoria> { entidad = categoria,
            mensaje = input.estado ? "Categoría activada exitosamente" : "Categoría desactivada exitosamente", exitoso = true });
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var categoria = await _context.Categorias.Include(c => c.Productos).FirstOrDefaultAsync(c => c.Id == id);
        if (categoria == null) return ServiceResult<object>.NotFound();
        if (categoria.Productos.Any(p => p.Estado == true))
        {
            categoria.Estado = false; await _context.SaveChangesAsync();
            return ServiceResult<object>.Ok(new { message = "Categoría desactivada (borrado lógico por tener productos activos asociados)", eliminado = true, fisico = false,
                productosAsociados = categoria.Productos.Count(p => p.Estado == true) });
        }
        _context.Categorias.Remove(categoria); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Categoría eliminada físicamente de la base de datos", eliminado = true, fisico = true });
    }
}
