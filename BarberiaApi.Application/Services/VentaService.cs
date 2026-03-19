using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BarberiaApi.Application.Services;

public class VentaService : IVentaService
{
    private readonly BarberiaContext _context;

    public VentaService(BarberiaContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? searchTerm)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;

        var baseQ = _context.Ventas.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            baseQ = baseQ.Where(v =>
                (v.Estado != null && v.Estado.ToLower().Contains(term)) ||
                (v.MetodoPago != null && v.MetodoPago.ToLower().Contains(term)) ||
                (v.Cliente != null && v.Cliente.Usuario != null && (
                    (v.Cliente.Usuario.Nombre != null && v.Cliente.Usuario.Nombre.ToLower().Contains(term)) ||
                    (v.Cliente.Usuario.Apellido != null && v.Cliente.Usuario.Apellido.ToLower().Contains(term))
                )) ||
                (v.Usuario != null && (
                    (v.Usuario.Nombre != null && v.Usuario.Nombre.ToLower().Contains(term)) ||
                    (v.Usuario.Apellido != null && v.Usuario.Apellido.ToLower().Contains(term))
                )) ||
                (v.Barbero != null && v.Barbero.Usuario != null && (
                    (v.Barbero.Usuario.Nombre != null && v.Barbero.Usuario.Nombre.ToLower().Contains(term)) ||
                    (v.Barbero.Usuario.Apellido != null && v.Barbero.Usuario.Apellido.ToLower().Contains(term))
                ))
            );
        }

        var totalCount = await baseQ.CountAsync();
        var items = await baseQ
            .OrderByDescending(v => v.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new
            {
                v.Id, v.Fecha, v.Subtotal, v.Total, v.Descuento, v.IVA,
                v.Estado, v.MetodoPago, v.ClienteId, v.BarberoId, v.UsuarioId, v.SaldoAFavorUsado,
                Cliente = v.Cliente == null ? null : new
                {
                    v.Cliente.Id, v.Cliente.UsuarioId, v.Cliente.Telefono,
                    Usuario = v.Cliente.Usuario == null ? null : new
                    { v.Cliente.Usuario.Id, v.Cliente.Usuario.Nombre, v.Cliente.Usuario.Apellido, v.Cliente.Usuario.Correo }
                },
                Usuario = v.Usuario == null ? null : new { v.Usuario.Id, v.Usuario.Nombre, v.Usuario.Apellido },
                Barbero = v.Barbero == null ? null : new
                {
                    v.Barbero.Id, v.Barbero.UsuarioId,
                    Usuario = v.Barbero.Usuario == null ? null : new
                    { v.Barbero.Usuario.Id, v.Barbero.Usuario.Nombre, v.Barbero.Usuario.Apellido }
                }
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var venta = await _context.Ventas
            .AsNoTracking().AsSplitQuery()
            .Include(v => v.Cliente).Include(v => v.Usuario).Include(v => v.Barbero)
            .Include(v => v.DetalleVenta).ThenInclude(d => d.Producto)
            .Include(v => v.DetalleVenta).ThenInclude(d => d.Servicio)
            .Include(v => v.DetalleVenta).ThenInclude(d => d.Paquete)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venta == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(venta);
    }

    public async Task<ServiceResult<object>> GetByAgendamientoAsync(int agendamientoId)
    {
        var ag = await _context.Agendamientos
            .Include(a => a.Barbero).Include(a => a.Cliente)
            .FirstOrDefaultAsync(a => a.Id == agendamientoId);

        if (ag == null) return ServiceResult<object>.NotFound();

        var usuarioId = ag.Barbero?.UsuarioId ?? 0;
        var ventaRelacionada = await _context.Ventas
            .Include(v => v.DetalleVenta)
            .Where(v => v.ClienteId == ag.ClienteId && v.UsuarioId == usuarioId)
            .Where(v => v.DetalleVenta.Any(d =>
                (ag.ServicioId.HasValue && d.ServicioId == ag.ServicioId) ||
                (ag.PaqueteId.HasValue && d.PaqueteId == ag.PaqueteId)))
            .OrderByDescending(v => v.Id)
            .FirstOrDefaultAsync();

        if (ventaRelacionada == null) return ServiceResult<object>.Ok(new { ventaId = 0 });
        return ServiceResult<object>.Ok(new
        {
            ventaId = ventaRelacionada.Id,
            venta = new
            {
                ventaRelacionada.Id, ventaRelacionada.ClienteId, ventaRelacionada.UsuarioId,
                ventaRelacionada.BarberoId, ventaRelacionada.Fecha, ventaRelacionada.Subtotal,
                ventaRelacionada.Total, ventaRelacionada.Estado, ventaRelacionada.MetodoPago
            }
        });
    }

    public async Task<ServiceResult<object>> CreateAsync(VentaInput input)
    {
        if (input == null || input.Detalles == null || !input.Detalles.Any())
            return ServiceResult<object>.Fail("La venta debe tener al menos un detalle");

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            bool tieneServicioDirecto = input.Detalles.Any(d => d.ServicioId.HasValue);
            bool paqueteConServicio = false;
            var paqueteIds = input.Detalles.Where(d => d.PaqueteId.HasValue).Select(d => d.PaqueteId!.Value).Distinct().ToList();
            if (paqueteIds.Count > 0)
                paqueteConServicio = await _context.DetallePaquetes.AnyAsync(dp => paqueteIds.Contains(dp.PaqueteId) && dp.ServicioId != null);

            bool requiereBarbero = tieneServicioDirecto || paqueteConServicio;
            if (requiereBarbero && !input.BarberoId.HasValue)
                return ServiceResult<object>.Fail("Se requiere BarberoId cuando la venta incluye servicios");

            int usuarioId = input.UsuarioId;
            if (requiereBarbero)
            {
                var barberoExistente = await _context.Barberos.FirstOrDefaultAsync(b => b.Id == input.BarberoId!.Value);
                if (barberoExistente == null)
                    return ServiceResult<object>.Fail("El barbero especificado no existe");
            }

            if (usuarioId == 0 && input.BarberoId.HasValue)
            {
                var barbero = await _context.Barberos.FirstOrDefaultAsync(b => b.Id == input.BarberoId.Value);
                if (barbero == null) return ServiceResult<object>.Fail("El barbero especificado no existe");
                usuarioId = barbero.UsuarioId;
                if (usuarioId == 0) return ServiceResult<object>.Fail("El barbero especificado no tiene usuario asociado valido");
            }
            else
            {
                var usuario = await _context.Usuarios.FindAsync(usuarioId);
                if (usuario == null) return ServiceResult<object>.Fail("El usuario especificado no existe");
            }

            var venta = new Venta
            {
                UsuarioId = usuarioId,
                ClienteId = input.ClienteId,
                BarberoId = input.BarberoId,
                Fecha = DateTime.Now,
                MetodoPago = input.MetodoPago ?? "Efectivo",
                Descuento = input.Descuento ?? 0,
                IVA = 0,
                Estado = "Completada"
            };

            foreach (var det in input.Detalles)
            {
                if (det.Cantidad <= 0)
                    return ServiceResult<object>.Fail("La cantidad de cada detalle debe ser mayor a 0");
                if (det.PrecioUnitario < 0)
                    return ServiceResult<object>.Fail("El precio unitario no puede ser negativo");
            }

            var ventaProductIds = input.Detalles.Where(d => d.ProductoId.HasValue).Select(d => d.ProductoId!.Value).Distinct().ToList();
            var ventaProductos = ventaProductIds.Count > 0
                ? await _context.Productos.Where(p => ventaProductIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id)
                : new Dictionary<int, Domain.Entities.Producto>();

            decimal subtotal = 0;
            foreach (var detInput in input.Detalles)
            {
                var detalle = new DetalleVenta
                {
                    ProductoId = detInput.ProductoId,
                    ServicioId = detInput.ServicioId,
                    PaqueteId = detInput.PaqueteId,
                    Cantidad = detInput.Cantidad,
                    PrecioUnitario = detInput.PrecioUnitario
                };
                detalle.Subtotal = detalle.Cantidad * detalle.PrecioUnitario;
                subtotal += detalle.Subtotal;

                if (detInput.ProductoId.HasValue)
                {
                    if (!ventaProductos.TryGetValue(detInput.ProductoId.Value, out var producto))
                        return ServiceResult<object>.Fail($"Producto {detInput.ProductoId} no encontrado");
                    if (producto.StockVentas < detInput.Cantidad)
                        return ServiceResult<object>.Fail($"Stock insuficiente para el producto {producto.Nombre}");
                    producto.StockVentas -= detInput.Cantidad;
                    producto.StockVentas = Math.Max(0, producto.StockVentas);
                    producto.StockTotal = producto.StockVentas + producto.StockInsumos;
                }
                venta.DetalleVenta.Add(detalle);
            }

            venta.Subtotal = subtotal;
            venta.Total = (subtotal + venta.IVA.Value) - venta.Descuento.Value;

            if (input.ClienteId.HasValue)
            {
                var clienteId = input.ClienteId.Value;
                var totalDevoluciones = await _context.Devoluciones
                    .Where(d => d.ClienteId == clienteId && (d.Estado == "Activo" || d.Estado == "Completada" || d.Estado == "Procesado"))
                    .SumAsync(d => d.SaldoAFavor ?? 0);
                var totalUsado = await _context.Ventas
                    .Where(v => v.ClienteId == clienteId && v.Estado != "Anulada")
                    .SumAsync(v => v.SaldoAFavorUsado ?? 0);
                var disponible = Math.Max(0, totalDevoluciones - totalUsado);

                var solicitado = input.SaldoAFavorUsado ?? 0;
                decimal aplicable = 0;
                if (solicitado > 0)
                    aplicable = Math.Min(solicitado, Math.Min(disponible, venta.Total));
                else if (input.UsarSaldoAFavor == true || input.SaldoAFavorUsado == null)
                    aplicable = Math.Min(disponible, venta.Total);

                if (aplicable > 0)
                {
                    venta.SaldoAFavorUsado = aplicable;
                    venta.Total = Math.Max(0, venta.Total - aplicable);
                }
            }

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var ventaCompleta = await _context.Ventas
                .Include(v => v.Cliente).Include(v => v.Usuario).Include(v => v.Barbero)
                .Include(v => v.DetalleVenta).ThenInclude(d => d.Producto)
                .Include(v => v.DetalleVenta).ThenInclude(d => d.Servicio)
                .Include(v => v.DetalleVenta).ThenInclude(d => d.Paquete)
                .FirstOrDefaultAsync(v => v.Id == venta.Id);

            return ServiceResult<object>.Ok(ventaCompleta!);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail($"Error interno: {ex.Message}", 500);
        }
    }

    public async Task<ServiceResult<object>> AnularAsync(int id)
    {
        var venta = await _context.Ventas
            .Include(v => v.DetalleVenta).ThenInclude(d => d.Producto)
            .Include(v => v.Cliente).Include(v => v.Usuario)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venta == null) return ServiceResult<object>.NotFound();
        if (venta.Estado == "Anulada") return ServiceResult<object>.Fail("La venta ya esta anulada");

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var estadoAnterior = venta.Estado;
            venta.Estado = "Anulada";
            if ((venta.SaldoAFavorUsado ?? 0) > 0) venta.SaldoAFavorUsado = 0;

            foreach (var detalle in venta.DetalleVenta)
            {
                if (detalle.ProductoId.HasValue)
                {
                    var producto = await _context.Productos.FindAsync(detalle.ProductoId.Value);
                    if (producto != null)
                    {
                        producto.StockVentas += detalle.Cantidad;
                        producto.StockTotal = producto.StockVentas + producto.StockInsumos;
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<object>.Ok(new CambioEstadoResponse<Venta>
            {
                entidad = venta,
                mensaje = $"Venta anulada exitosamente. Estado anterior: '{estadoAnterior}'",
                exitoso = true
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail($"Error interno: {ex.Message}", 500);
        }
    }
}
