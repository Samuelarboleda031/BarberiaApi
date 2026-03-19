using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class ServicioService : IServicioService
{
    private readonly BarberiaContext _context;
    public ServicioService(BarberiaContext context) => _context = context;

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1; if (pageSize < 1) pageSize = 5;
        var baseQ = _context.Servicios.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(s => (s.Nombre != null && s.Nombre.ToLower().Contains(term)) || (s.Descripcion != null && s.Descripcion.ToLower().Contains(term)));
        }
        var totalCount = await baseQ.CountAsync();
        var items = await baseQ.OrderBy(s => s.Nombre).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var servicio = await _context.Servicios.Include(s => s.DetallePaquetes).FirstOrDefaultAsync(s => s.Id == id);
        if (servicio == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(servicio);
    }

    public async Task<ServiceResult<object>> CreateAsync(Servicio servicio)
    {
        if (servicio == null) return ServiceResult<object>.Fail("El objeto servicio es requerido");
        if (string.IsNullOrWhiteSpace(servicio.Nombre)) return ServiceResult<object>.Fail("El nombre del servicio es requerido");
        if (servicio.Precio <= 0) return ServiceResult<object>.Fail("El precio debe ser mayor a cero");
        if (!Helpers.ValidationHelper.ValidarUrlImagen(servicio.Imagen, out var imgError)) return ServiceResult<object>.Fail(imgError!);
        servicio.Id = 0; servicio.Estado = true;
        _context.Servicios.Add(servicio); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(servicio);
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, Servicio servicio)
    {
        if (id != servicio.Id) return ServiceResult<object>.Fail("Id no coincide");
        var existing = await _context.Servicios.FindAsync(id);
        if (existing == null) return ServiceResult<object>.NotFound();
        if (!Helpers.ValidationHelper.ValidarUrlImagen(servicio.Imagen, out var imgError)) return ServiceResult<object>.Fail(imgError!);
        _context.Entry(existing).CurrentValues.SetValues(servicio);
        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Servicio actualizado" });
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input)
    {
        var servicio = await _context.Servicios.Include(s => s.DetallePaquetes).FirstOrDefaultAsync(s => s.Id == id);
        if (servicio == null) return ServiceResult<object>.NotFound();
        servicio.Estado = input.estado; await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new CambioEstadoResponse<Servicio> { entidad = servicio,
            mensaje = input.estado ? "Servicio activado exitosamente" : "Servicio desactivado exitosamente", exitoso = true });
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var servicio = await _context.Servicios.Include(s => s.Agendamientos).Include(s => s.DetalleVenta).Include(s => s.DetallePaquetes).FirstOrDefaultAsync(s => s.Id == id);
        if (servicio == null) return ServiceResult<object>.NotFound();
        bool tieneAgendamientosActivos = servicio.Agendamientos.Any(a => a.Estado != "Cancelada");
        bool tieneVentas = servicio.DetalleVenta.Any();
        bool estaEnPaquetes = servicio.DetallePaquetes.Any();
        if (tieneAgendamientosActivos || tieneVentas || estaEnPaquetes)
        {
            servicio.Estado = false; await _context.SaveChangesAsync();
            return ServiceResult<object>.Ok(new { message = "Servicio desactivado (borrado lógico por tener registros asociados)", eliminado = true, fisico = false,
                motivo = tieneAgendamientosActivos ? "Agendamientos activos" : tieneVentas ? "Ventas registradas" : "Incluido en paquetes" });
        }
        _context.Servicios.Remove(servicio); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Servicio eliminado físicamente de la base de datos", eliminado = true, fisico = true });
    }
}
