using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class EntregaInsumoService : IEntregaInsumoService
{
    private readonly BarberiaContext _context;

    public EntregaInsumoService(BarberiaContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;
        var baseQ = _context.EntregasInsumos
            .Include(e => e.Barbero).ThenInclude(b => b.Usuario)
            .Include(e => e.Usuario)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(e =>
                (e.Estado != null && e.Estado.ToLower().Contains(term)) ||
                (e.Usuario != null && e.Usuario.Nombre != null && e.Usuario.Nombre.ToLower().Contains(term)) ||
                (e.Barbero != null && e.Barbero.Usuario != null && (
                    (e.Barbero.Usuario.Nombre != null && e.Barbero.Usuario.Nombre.ToLower().Contains(term)) ||
                    (e.Barbero.Usuario.Apellido != null && e.Barbero.Usuario.Apellido.ToLower().Contains(term))
                ))
            );
        }
        var totalCount = await baseQ.CountAsync();
        var items = await baseQ
            .OrderByDescending(e => e.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetDevolucionesAsync(int? barberoId, int? entregaId, DateTime? desde, DateTime? hasta, int page, int pageSize, string? q)
    {
        var query = _context.EntregasInsumos
            .Include(e => e.Barbero).ThenInclude(b => b.Usuario)
            .Include(e => e.Usuario)
            .Include(e => e.DetalleEntregasInsumos).ThenInclude(d => d.Producto)
            .Where(e => _context.Devoluciones.Any(dev => dev.EntregaId == e.Id))
            .AsQueryable();

        if (barberoId.HasValue) query = query.Where(e => e.BarberoId == barberoId.Value);
        if (entregaId.HasValue) query = query.Where(e => e.Id == entregaId.Value);
        if (desde.HasValue) query = query.Where(e => e.Fecha >= desde.Value);
        if (hasta.HasValue)
        {
            var h = hasta.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(e => e.Fecha <= h);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(e =>
                (e.Estado != null && e.Estado.ToLower().Contains(term)) ||
                (e.Usuario != null && e.Usuario.Nombre != null && e.Usuario.Nombre.ToLower().Contains(term)) ||
                (e.Barbero != null && e.Barbero.Usuario != null && (
                    (e.Barbero.Usuario.Nombre != null && e.Barbero.Usuario.Nombre.ToLower().Contains(term)) ||
                    (e.Barbero.Usuario.Apellido != null && e.Barbero.Usuario.Apellido.ToLower().Contains(term))
                )) ||
                e.DetalleEntregasInsumos.Any(d => d.Producto != null && d.Producto.Nombre != null && d.Producto.Nombre.ToLower().Contains(term))
            );
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;
        var queryOrdered = query.OrderByDescending(e => e.Fecha);
        var totalCount = await queryOrdered.CountAsync();
        var items = await queryOrdered
            .Select(e => new
            {
                e.Id,
                e.Fecha,
                e.Estado,
                e.CantidadTotal,
                e.ValorTotal,
                Barbero = new
                {
                    e.BarberoId,
                    Nombre = e.Barbero.Usuario != null ? (e.Barbero.Usuario.Nombre + " " + e.Barbero.Usuario.Apellido) : null
                },
                Usuario = new
                {
                    e.UsuarioId,
                    e.Usuario.Nombre
                },
                Detalles = e.DetalleEntregasInsumos.Select(d => new
                {
                    d.ProductoId,
                    Producto = d.Producto.Nombre,
                    d.Cantidad,
                    d.PrecioHistorico
                })
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetDevolucionesResumenAsync(int? barberoId, int? entregaId, DateTime? desde, DateTime? hasta)
    {
        var q = _context.EntregasInsumos
            .Include(e => e.Barbero).ThenInclude(b => b.Usuario)
            .Where(e => _context.Devoluciones.Any(dev => dev.EntregaId == e.Id))
            .AsQueryable();

        if (barberoId.HasValue) q = q.Where(e => e.BarberoId == barberoId.Value);
        if (entregaId.HasValue) q = q.Where(e => e.Id == entregaId.Value);
        if (desde.HasValue) q = q.Where(e => e.Fecha >= desde.Value);
        if (hasta.HasValue)
        {
            var h = hasta.Value.Date.AddDays(1).AddTicks(-1);
            q = q.Where(e => e.Fecha <= h);
        }

        var totalCantidad = await q.SumAsync(e => (int?)e.CantidadTotal) ?? 0;
        var totalValor = await q.SumAsync(e => (decimal?)e.ValorTotal) ?? 0m;

        var porBarbero = await q
            .GroupBy(e => new
            {
                e.BarberoId,
                Nombre = e.Barbero.Usuario != null ? (e.Barbero.Usuario.Nombre + " " + e.Barbero.Usuario.Apellido) : null
            })
            .Select(g => new
            {
                g.Key.BarberoId,
                g.Key.Nombre,
                CantidadTotal = g.Sum(x => x.CantidadTotal),
                ValorTotal = g.Sum(x => x.ValorTotal)
            })
            .OrderByDescending(x => x.CantidadTotal)
            .ToListAsync();

        return ServiceResult<object>.Ok(new { totalCantidad, totalValor, porBarbero });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var entrega = await _context.EntregasInsumos
            .Include(e => e.Barbero)
            .Include(e => e.Usuario)
            .Include(e => e.DetalleEntregasInsumos)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entrega == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(entrega);
    }

    public async Task<ServiceResult<object>> CreateAsync(EntregaInput input)
    {
        if (input == null || input.Detalles == null || !input.Detalles.Any())
            return ServiceResult<object>.Fail("La entrega debe tener al menos un detalle");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var entrega = new EntregasInsumo
            {
                BarberoId = input.BarberoId,
                UsuarioId = input.UsuarioId,
                Fecha = DateTime.Now,
                Estado = "Entregado"
            };

            int cantidadTotal = 0;
            decimal valorTotal = 0;

            foreach (var detInput in input.Detalles)
            {
                var producto = await _context.Productos.FindAsync(detInput.ProductoId);
                if (producto == null) return ServiceResult<object>.Fail($"Producto {detInput.ProductoId} no encontrado");

                if (producto.StockInsumos < detInput.Cantidad)
                    return ServiceResult<object>.Fail($"Stock insuficiente para el producto {producto.Nombre}");

                var detalle = new DetalleEntregasInsumo
                {
                    ProductoId = detInput.ProductoId,
                    Cantidad = detInput.Cantidad,
                    PrecioHistorico = producto.PrecioVenta
                };

                cantidadTotal += detInput.Cantidad;
                valorTotal += (detalle.PrecioHistorico ?? 0) * detInput.Cantidad;

                producto.StockInsumos = Math.Max(0, producto.StockInsumos - detInput.Cantidad);
                producto.StockTotal = producto.StockVentas + producto.StockInsumos;

                entrega.DetalleEntregasInsumos.Add(detalle);
            }

            entrega.CantidadTotal = cantidadTotal;
            entrega.ValorTotal = valorTotal;

            _context.EntregasInsumos.Add(entrega);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<object>.Ok(entrega);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail($"Error interno: {ex.Message}", 500);
        }
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoInput input)
    {
        var entrega = await _context.EntregasInsumos
            .Include(e => e.Barbero)
            .Include(e => e.Usuario)
            .Include(e => e.DetalleEntregasInsumos)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entrega == null) return ServiceResult<object>.NotFound();

        if (string.IsNullOrWhiteSpace(input.estado))
            return ServiceResult<object>.Fail("El estado es requerido");

        var estadosValidos = new[] { "Entregado", "Anulado", "Pendiente", "Parcial" };
        if (!estadosValidos.Contains(input.estado))
            return ServiceResult<object>.Fail($"Estado no válido. Estados permitidos: {string.Join(", ", estadosValidos)}");

        if (input.estado == "Anulado" && entrega.Estado == "Anulado")
            return ServiceResult<object>.Fail("La entrega ya está anulada");

        if (input.estado == "Entregado" && entrega.Estado == "Anulado")
            return ServiceResult<object>.Fail("No se puede reactivar una entrega anulada");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var estadoAnterior = entrega.Estado;
            entrega.Estado = input.estado;

            if (input.estado == "Anulado" && estadoAnterior != "Anulado")
            {
                foreach (var detalle in entrega.DetalleEntregasInsumos)
                {
                    var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                    if (producto != null)
                    {
                        producto.StockInsumos += detalle.Cantidad;
                        producto.StockTotal = producto.StockVentas + producto.StockInsumos;
                    }
                }
            }
            else if (input.estado == "Entregado" && estadoAnterior != "Anulado" && estadoAnterior != "Entregado")
            {
                foreach (var detalle in entrega.DetalleEntregasInsumos)
                {
                    var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                    if (producto != null)
                    {
                        if (producto.StockInsumos >= detalle.Cantidad)
                        {
                            producto.StockInsumos = Math.Max(0, producto.StockInsumos - detalle.Cantidad);
                            producto.StockTotal = producto.StockVentas + producto.StockInsumos;
                        }
                        else
                        {
                            await transaction.RollbackAsync();
                            return ServiceResult<object>.Fail($"Stock insuficiente para reactivar la entrega. Producto: {producto.Nombre}, Stock actual: {producto.StockInsumos}, Requerido: {detalle.Cantidad}");
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = new CambioEstadoResponse<EntregasInsumo>
            {
                entidad = entrega,
                mensaje = $"Entrega actualizada de '{estadoAnterior}' a '{input.estado}' exitosamente",
                exitoso = true
            };

            return ServiceResult<object>.Ok(response);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail($"Error interno: {ex.Message}", 500);
        }
    }
}
