using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class PaqueteService : IPaqueteService
{
    private readonly BarberiaContext _context;
    public PaqueteService(BarberiaContext context) => _context = context;

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1; if (pageSize < 1) pageSize = 5;
        var baseQ = _context.Paquetes.Include(p => p.DetallePaquetes).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(p => (p.Nombre != null && p.Nombre.ToLower().Contains(term)) || (p.Descripcion != null && p.Descripcion.ToLower().Contains(term)));
        }
        var totalCount = await baseQ.CountAsync();
        var items = await baseQ.OrderBy(p => p.Nombre).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var paquete = await _context.Paquetes.AsNoTracking().AsSplitQuery().Include(p => p.DetallePaquetes).ThenInclude(d => d.Servicio).FirstOrDefaultAsync(p => p.Id == id);
        if (paquete == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(paquete);
    }

    public async Task<ServiceResult<object>> CreateAsync(PaqueteInput input)
    {
        if (input == null) return ServiceResult<object>.Fail("El objeto paquete es requerido");
        if (string.IsNullOrWhiteSpace(input.Nombre)) return ServiceResult<object>.Fail("El nombre es requerido");
        var paquete = new Paquete { Nombre = input.Nombre, Descripcion = input.Descripcion, Precio = input.Precio, DuracionMinutos = input.DuracionMinutos, Estado = true };
        _context.Paquetes.Add(paquete); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(paquete);
    }

    public async Task<ServiceResult<object>> CreateCompletoAsync(PaqueteConDetallesInput input)
    {
        if (input == null) return ServiceResult<object>.Fail("El objeto paquete es requerido");
        if (string.IsNullOrWhiteSpace(input.Nombre)) return ServiceResult<object>.Fail("El nombre es requerido");
        if (input.Detalles == null || !input.Detalles.Any()) return ServiceResult<object>.Fail("El paquete debe tener al menos un detalle");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var paquete = new Paquete { Nombre = input.Nombre, Descripcion = input.Descripcion, Precio = input.Precio, DuracionMinutos = input.DuracionMinutos, Estado = true };
            foreach (var det in input.Detalles)
            {
                if (det.ServicioId <= 0 || det.Cantidad <= 0) return ServiceResult<object>.Fail("ServicioId y Cantidad son obligatorios y mayores a cero");
                if (!await _context.Servicios.AnyAsync(s => s.Id == det.ServicioId)) return ServiceResult<object>.Fail($"El servicio con id {det.ServicioId} no existe");
                paquete.DetallePaquetes.Add(new DetallePaquete { ServicioId = det.ServicioId, Cantidad = det.Cantidad });
            }
            _context.Paquetes.Add(paquete); await _context.SaveChangesAsync(); await transaction.CommitAsync();
            var resultado = await _context.Paquetes.Include(p => p.DetallePaquetes).ThenInclude(d => d.Servicio).FirstOrDefaultAsync(p => p.Id == paquete.Id);
            return ServiceResult<object>.Ok(resultado!);
        }
        catch (Exception ex) { await transaction.RollbackAsync(); return ServiceResult<object>.Fail($"Error interno: {ex.Message}", 500); }
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, Paquete paquete)
    {
        if (id != paquete.Id) return ServiceResult<object>.Fail("Id no coincide");
        var existing = await _context.Paquetes.FindAsync(id);
        if (existing == null) return ServiceResult<object>.NotFound();
        _context.Entry(existing).CurrentValues.SetValues(paquete);
        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Paquete actualizado" });
    }

    public async Task<ServiceResult<object>> UpdateDetallesAsync(int id, List<DetallePaqueteSimpleInput> detalles)
    {
        var paquete = await _context.Paquetes.Include(p => p.DetallePaquetes).FirstOrDefaultAsync(p => p.Id == id);
        if (paquete == null) return ServiceResult<object>.NotFound();
        if (detalles == null || !detalles.Any()) return ServiceResult<object>.Fail("El paquete debe tener al menos un detalle");
        foreach (var det in detalles)
        {
            if (det.ServicioId <= 0 || det.Cantidad <= 0) return ServiceResult<object>.Fail("ServicioId y Cantidad son obligatorios y mayores a cero");
            if (!await _context.Servicios.AnyAsync(s => s.Id == det.ServicioId)) return ServiceResult<object>.Fail($"El servicio con id {det.ServicioId} no existe");
        }
        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.DetallePaquetes.RemoveRange(paquete.DetallePaquetes); await _context.SaveChangesAsync();
            foreach (var det in detalles) _context.DetallePaquetes.Add(new DetallePaquete { PaqueteId = id, ServicioId = det.ServicioId, Cantidad = det.Cantidad });
            await _context.SaveChangesAsync(); await tx.CommitAsync();
            var resultado = await _context.Paquetes.Include(p => p.DetallePaquetes).ThenInclude(d => d.Servicio).FirstOrDefaultAsync(p => p.Id == id);
            return ServiceResult<object>.Ok(resultado!);
        }
        catch (Exception ex) { await tx.RollbackAsync(); return ServiceResult<object>.Fail($"Error interno: {ex.Message}", 500); }
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input)
    {
        var paquete = await _context.Paquetes.Include(p => p.DetallePaquetes).ThenInclude(d => d.Servicio).FirstOrDefaultAsync(p => p.Id == id);
        if (paquete == null) return ServiceResult<object>.NotFound();
        paquete.Estado = input.estado; await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new CambioEstadoResponse<Paquete> { entidad = paquete,
            mensaje = input.estado ? "Paquete activado exitosamente" : "Paquete desactivado exitosamente", exitoso = true });
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var paquete = await _context.Paquetes.Include(p => p.DetallePaquetes).Include(p => p.Agendamientos).FirstOrDefaultAsync(p => p.Id == id);
        if (paquete == null) return ServiceResult<object>.NotFound();
        bool tieneVentas = await _context.DetalleVentas.AnyAsync(d => d.PaqueteId == id);
        bool tieneAgendamientosActivos = paquete.Agendamientos.Any(a => a.Estado != "Cancelada");
        if (tieneVentas || tieneAgendamientosActivos)
        {
            paquete.Estado = false; await _context.SaveChangesAsync();
            return ServiceResult<object>.Ok(new { message = "Paquete desactivado (borrado lógico por tener registros asociados)", eliminado = true, fisico = false,
                motivo = tieneVentas ? "Ventas registradas" : "Agendamientos activos", detallesAsociados = paquete.DetallePaquetes.Count() });
        }
        _context.DetallePaquetes.RemoveRange(paquete.DetallePaquetes);
        _context.Paquetes.Remove(paquete); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Paquete eliminado físicamente de la base de datos", eliminado = true, fisico = true });
    }
}
