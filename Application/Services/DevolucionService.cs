using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BarberiaApi.Application.Services;

public class DevolucionService : IDevolucionService
{
    private readonly BarberiaContext _context;

    public DevolucionService(BarberiaContext context)
    {
        _context = context;
    }

    private static bool IsErrorCompra(string? categoria, string? detalle)
    {
        var cat = (categoria ?? string.Empty).Trim().ToLower();
        var normalized = cat.Replace("_", "").Replace("-", "").Replace(" ", "");
        if (normalized == "errorcompra" || normalized == "errorenlacompra") return true;
        var det = (detalle ?? string.Empty).Trim().ToLower();
        return det.Contains("error") && det.Contains("compra");
    }

    public async Task<ServiceResult<object>> GetAllAsync(int? clienteId, int? productoId, DateTime? desde, DateTime? hasta, int page, int pageSize, string? q)
    {
        try
        {
            var query = _context.Devoluciones
                .AsNoTracking()
                .AsQueryable();

            if (clienteId.HasValue) query = query.Where(d => d.ClienteId == clienteId.Value);
            if (productoId.HasValue) query = query.Where(d => d.ProductoId == productoId.Value);
            if (desde.HasValue) query = query.Where(d => d.Fecha >= desde.Value);
            if (hasta.HasValue)
            {
                var h = hasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(d => d.Fecha <= h);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(d =>
                    (d.MotivoCategoria != null && d.MotivoCategoria.ToLower().Contains(term)) ||
                    (d.MotivoDetalle != null && d.MotivoDetalle.ToLower().Contains(term)) ||
                    (d.Observaciones != null && d.Observaciones.ToLower().Contains(term)) ||
                    (d.Estado != null && d.Estado.ToLower().Contains(term)) ||
                    (d.Producto != null && d.Producto.Nombre != null && d.Producto.Nombre.ToLower().Contains(term)) ||
                    (d.Usuario != null && d.Usuario.Nombre != null && d.Usuario.Nombre.ToLower().Contains(term)) ||
                    (d.Cliente != null && d.Cliente.Usuario != null && (
                        (d.Cliente.Usuario.Nombre != null && d.Cliente.Usuario.Nombre.ToLower().Contains(term)) ||
                        (d.Cliente.Usuario.Apellido != null && d.Cliente.Usuario.Apellido.ToLower().Contains(term))
                    ))
                );
            }

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;

            var totalCount = await query.CountAsync();
            var items = await query
                    .OrderByDescending(d => d.Fecha)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(d => new
                    {
                        d.Id,
                        d.VentaId,
                        d.ClienteId,
                        d.UsuarioId,
                        d.ProductoId,
                        d.Cantidad,
                        d.MotivoCategoria,
                        d.MotivoDetalle,
                        d.Observaciones,
                        d.MontoDevuelto,
                        d.SaldoAFavor,
                        d.Fecha,
                        d.Estado,
                        ProductoNombre = d.Producto != null ? d.Producto.Nombre : "Producto",
                        UsuarioNombre = d.Usuario != null ? d.Usuario.Nombre : "Sistema",
                        ClienteNombre = d.Cliente != null && d.Cliente.Usuario != null ? d.Cliente.Usuario.Nombre + " " + d.Cliente.Usuario.Apellido : null
                    })
                    .ToListAsync();

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.Fail($"Error al obtener devoluciones: {ex.Message} | {ex.InnerException?.Message}", 500);
        }
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var devolucion = await _context.Devoluciones
            .Where(d => d.Id == id)
            .Select(d => new
            {
                d.Id,
                d.VentaId,
                d.ClienteId,
                d.UsuarioId,
                d.ProductoId,
                d.Cantidad,
                d.MotivoCategoria,
                d.MotivoDetalle,
                d.Observaciones,
                d.MontoDevuelto,
                d.SaldoAFavor,
                d.Fecha,
                d.Estado,
                Producto = d.ProductoId.HasValue ? new { d.Producto.Id, d.Producto.Nombre } : null,
                Usuario = new { d.Usuario.Id, d.Usuario.Nombre },
                Cliente = d.ClienteId.HasValue && d.Cliente != null && d.Cliente.Usuario != null
                    ? new { d.Cliente.Id, Nombre = d.Cliente.Usuario.Nombre }
                    : null
            })
            .FirstOrDefaultAsync();

        if (devolucion == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(devolucion);
    }

    public async Task<ServiceResult<object>> CreateAsync(DevolucionInput input)
    {
        if (input == null) return ServiceResult<object>.Fail("Input requerido");

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if (!input.VentaId.HasValue)
                return ServiceResult<object>.Fail("VentaId es requerido para devoluciones de ventas");

            var venta = await _context.Ventas
                .FirstOrDefaultAsync(v => v.Id == input.VentaId.Value);

            if (venta == null)
                return ServiceResult<object>.Fail("La venta no existe");

            var fechaVenta = venta.Fecha ?? DateTime.Now;
            var exp = fechaVenta.AddDays(15);
            if (DateTime.Now > exp)
                return ServiceResult<object>.Fail("Garantía expirada para esta venta");

            var vendido = await _context.DetalleVentas
                .Where(d => d.VentaId == input.VentaId.Value && d.ProductoId == input.ProductoId)
                .SumAsync(d => (int?)d.Cantidad) ?? 0;
            if (vendido <= 0)
                return ServiceResult<object>.Fail("El producto indicado no figura como vendido en esta venta.");

            var yaDevuelto = await _context.Devoluciones
                .Where(d => d.VentaId == input.VentaId.Value && d.ProductoId == input.ProductoId && d.Estado != "Anulado")
                .SumAsync(d => (int?)d.Cantidad) ?? 0;
            var disponible = vendido - yaDevuelto;
            if (disponible <= 0)
                return ServiceResult<object>.Fail("No hay cantidad disponible para devolver de este producto en la venta seleccionada.");
            if (input.Cantidad > disponible)
                return ServiceResult<object>.Fail($"Cantidad a devolver ({input.Cantidad}) excede disponible ({disponible}). Vendidos: {vendido}, ya devueltos: {yaDevuelto}.");

            bool esErrorCompraVenta = IsErrorCompra(input.MotivoCategoria, input.MotivoDetalle);

            var devolucion = new Devolucion
            {
                VentaId = input.VentaId,
                ClienteId = input.ClienteId ?? venta.ClienteId,
                UsuarioId = input.UsuarioId,
                ProductoId = input.ProductoId,
                Cantidad = input.Cantidad,
                MotivoCategoria = input.MotivoCategoria,
                MotivoDetalle = input.MotivoDetalle,
                Observaciones = input.Observaciones,
                MontoDevuelto = input.MontoDevuelto,
                SaldoAFavor = input.SaldoAFavor,
                Fecha = DateTime.Now,
                Estado = "Activo"
            };

            if (esErrorCompraVenta && input.ProductoId > 0)
            {
                var producto = await _context.Productos.FindAsync(input.ProductoId);
                if (producto != null)
                {
                    producto.Stock += input.Cantidad;
                }
            }

            _context.Devoluciones.Add(devolucion);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<object>.Ok(devolucion);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail($"Error interno: {ex.Message}", 500);
        }
    }

    public async Task<ServiceResult<object>> CreateBatchAsync(DevolucionBatchInput input)
    {
        if (input == null || input.Items == null || input.Items.Count == 0)
            return ServiceResult<object>.Fail("No hay items para registrar");

        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var venta = await _context.Ventas.FindAsync(input.VentaId);
            if (venta == null) return ServiceResult<object>.Fail("La venta no existe");

            var clienteId = input.ClienteId ?? venta.ClienteId;
            if (clienteId == null) return ServiceResult<object>.Fail("Venta no asociada a cliente");

            var fechaVenta = venta.Fecha ?? DateTime.Now;
            var exp = fechaVenta.AddDays(15);
            if (DateTime.Now > exp) return ServiceResult<object>.Fail("Garantía expirada para esta venta");

            bool esErrorCompraBatch = IsErrorCompra(input.MotivoCategoria, input.Observaciones);

            var vendidosPorProducto = await _context.DetalleVentas
                .Where(d => d.VentaId == input.VentaId)
                .GroupBy(d => d.ProductoId)
                .Select(g => new { ProductoId = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .ToDictionaryAsync(x => x.ProductoId ?? 0, x => x.Cantidad);

            var yaDevueltosPorProducto = await _context.Devoluciones
                .Where(d => d.VentaId == input.VentaId && d.Estado != "Anulado" && d.ProductoId.HasValue)
                .GroupBy(d => d.ProductoId)
                .Select(g => new { ProductoId = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .ToDictionaryAsync(x => x.ProductoId ?? 0, x => x.Cantidad);

            var propuestosPorProducto = input.Items
                .GroupBy(i => i.ProductoId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));

            foreach (var kv in propuestosPorProducto)
            {
                var pid = kv.Key;
                var propuesto = kv.Value;
                var vendidos = vendidosPorProducto.ContainsKey(pid) ? vendidosPorProducto[pid] : 0;
                if (vendidos <= 0)
                    return ServiceResult<object>.Fail($"El producto {pid} no figura como vendido en esta venta.");
                var yaDev = yaDevueltosPorProducto.ContainsKey(pid) ? yaDevueltosPorProducto[pid] : 0;
                var disponible = vendidos - yaDev;
                if (disponible <= 0)
                    return ServiceResult<object>.Fail($"No hay cantidad disponible para devolver del producto {pid} en esta venta.");
                if (propuesto > disponible)
                    return ServiceResult<object>.Fail($"Cantidad propuesta a devolver para producto {pid} ({propuesto}) excede disponible ({disponible}). Vendidos: {vendidos}, ya devueltos: {yaDev}.");
            }

            Dictionary<int, Domain.Entities.Producto> batchProductos = null!;
            if (esErrorCompraBatch)
            {
                var batchPids = input.Items.Select(i => i.ProductoId).Distinct().ToList();
                batchProductos = await _context.Productos.Where(p => batchPids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
            }

            foreach (var it in input.Items)
            {
                var dev = new Devolucion
                {
                    VentaId = input.VentaId,
                    ClienteId = clienteId,
                    UsuarioId = input.UsuarioId,
                    ProductoId = it.ProductoId,
                    Cantidad = it.Cantidad,
                    MotivoCategoria = input.MotivoCategoria,
                    MotivoDetalle = input.MotivoDetalle,
                    Observaciones = input.Observaciones,
                    MontoDevuelto = it.MontoDevuelto,
                    SaldoAFavor = it.MontoDevuelto,
                    Fecha = DateTime.Now,
                    Estado = "Activo"
                };

                _context.Devoluciones.Add(dev);

                if (esErrorCompraBatch && batchProductos.TryGetValue(it.ProductoId, out var p))
                {
                    p.Stock += it.Cantidad;
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult<object>.Ok(new { exitoso = true });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServiceResult<object>.Fail(ex.Message, 500);
        }
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoInput input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.estado))
            return ServiceResult<object>.Fail("El estado es requerido");

        var estadosValidos = new[] { "Activo", "Anulado", "Pendiente", "Procesado" };
        if (!estadosValidos.Contains(input.estado, StringComparer.OrdinalIgnoreCase))
            return ServiceResult<object>.Fail($"Estado '{input.estado}' no es válido. Estados permitidos: {string.Join(", ", estadosValidos)}");

        try
        {
            var devolucion = await _context.Devoluciones
                .Include(d => d.Producto)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (devolucion == null)
                return ServiceResult<object>.NotFound();

            var estadoAnterior = devolucion.Estado ?? "Pendiente";
            devolucion.Estado = input.estado;

            if (devolucion.ProductoId.HasValue && devolucion.ProductoId.Value > 0 && devolucion.Producto != null)
            {
                bool esErrorCompra = IsErrorCompra(devolucion.MotivoCategoria, devolucion.MotivoDetalle);

                bool esAnulacion = string.Equals(input.estado, "Anulado", StringComparison.OrdinalIgnoreCase) &&
                                  string.Equals(estadoAnterior, "Activo", StringComparison.OrdinalIgnoreCase);

                bool esReactivacion = string.Equals(input.estado, "Activo", StringComparison.OrdinalIgnoreCase) &&
                                     string.Equals(estadoAnterior, "Anulado", StringComparison.OrdinalIgnoreCase);

                if (esErrorCompra && devolucion.VentaId.HasValue)
                {
                    if (esAnulacion)
                    {
                        if (devolucion.Producto.Stock >= devolucion.Cantidad)
                        {
                            devolucion.Producto.Stock -= devolucion.Cantidad;
                        }
                        else
                        {
                            return ServiceResult<object>.Fail($"Stock insuficiente. Stock actual: {devolucion.Producto.Stock}, requerido: {devolucion.Cantidad}");
                        }
                    }
                    else if (esReactivacion)
                    {
                        devolucion.Producto.Stock += devolucion.Cantidad;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return ServiceResult<object>.Ok(new
            {
                mensaje = $"Devolución actualizada a estado: {input.estado}",
                exitoso = true,
                id = devolucion.Id,
                nuevoEstado = devolucion.Estado,
                estadoAnterior = estadoAnterior
            });
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.Fail($"Error interno al cambiar estado: {ex.Message}", 500);
        }
    }

    public async Task<ServiceResult<object>> GetByClienteAsync(int clienteId, int page, int pageSize)
    {
        return await GetAllAsync(
            clienteId: clienteId,
            productoId: null,
            desde: null,
            hasta: null,
            page: page,
            pageSize: pageSize,
            q: null);
    }

    public async Task<ServiceResult<object>> AnularAsync(int id)
    {
        var devolucion = await _context.Devoluciones
            .Include(d => d.Producto)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (devolucion == null) return ServiceResult<object>.NotFound();
        if (string.Equals(devolucion.Estado, "Anulado", StringComparison.OrdinalIgnoreCase))
            return ServiceResult<object>.Fail("La devolución ya está anulada", 409);

        var input = new CambioEstadoInput { estado = "Anulado" };
        return await CambiarEstadoAsync(id, input);
    }
}
