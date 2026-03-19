using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class CompraService : ICompraService
{
    private readonly BarberiaContext _context;

    public CompraService(BarberiaContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? searchTerm)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;

        var baseQ = _context.Compras.Include(c => c.Proveedor).Include(c => c.Usuario).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            baseQ = baseQ.Where(c =>
                (c.NumeroFactura != null && c.NumeroFactura.ToLower().Contains(term)) ||
                (c.MetodoPago != null && c.MetodoPago.ToLower().Contains(term)) ||
                (c.Proveedor != null && (
                    (c.Proveedor.Nombre != null && c.Proveedor.Nombre.ToLower().Contains(term)) ||
                    (c.Proveedor.NIT != null && c.Proveedor.NIT.ToLower().Contains(term))
                )) ||
                (c.Usuario != null && (
                    (c.Usuario.Nombre != null && c.Usuario.Nombre.ToLower().Contains(term)) ||
                    (c.Usuario.Apellido != null && c.Usuario.Apellido.ToLower().Contains(term))
                ))
            );
        }

        var totalCount = await baseQ.CountAsync();
        var items = await baseQ
            .OrderByDescending(c => c.FechaRegistro)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var compra = await _context.Compras
            .Include(c => c.Proveedor).Include(c => c.Usuario)
            .Include(c => c.DetalleCompras).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(compra);
    }

    public async Task<ServiceResult<object>> CreateAsync(CompraInput input)
    {
        if (input == null || input.Detalles == null || !input.Detalles.Any())
            return ServiceResult<object>.Fail("La compra debe tener al menos un detalle");

        var proveedorVal = await _context.Proveedores.FindAsync(input.ProveedorId);
        if (proveedorVal == null) return ServiceResult<object>.Fail("El proveedor no existe");
        if (proveedorVal.Estado.HasValue && !proveedorVal.Estado.Value)
            return ServiceResult<object>.Fail("El proveedor esta inactivo");

        var usuarioVal = await _context.Usuarios.FindAsync(input.UsuarioId);
        if (usuarioVal == null) return ServiceResult<object>.Fail("El usuario no existe");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var compra = new Compra
            {
                ProveedorId = input.ProveedorId,
                UsuarioId = input.UsuarioId,
                NumeroFactura = input.NumeroFactura,
                FechaFactura = input.FechaFactura.HasValue ? DateOnly.FromDateTime(input.FechaFactura.Value) : DateOnly.FromDateTime(DateTime.Now),
                FechaRegistro = DateTime.Now,
                MetodoPago = input.MetodoPago,
                IVA = input.IVA ?? 0,
                Descuento = input.Descuento ?? 0,
                Estado = "Completada"
            };

            decimal subtotal = 0;
            foreach (var detInput in input.Detalles)
            {
                var producto = await _context.Productos.FindAsync(detInput.ProductoId);
                if (producto == null)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<object>.Fail($"El producto {detInput.ProductoId} no existe");
                }

                var detalle = new DetalleCompra
                {
                    ProductoId = detInput.ProductoId,
                    Cantidad = detInput.Cantidad,
                    CantidadVentas = detInput.CantidadVentas,
                    CantidadInsumos = detInput.CantidadInsumos,
                    PrecioUnitario = detInput.PrecioUnitario,
                    Subtotal = detInput.Cantidad * detInput.PrecioUnitario
                };
                subtotal += detalle.Subtotal;

                producto.StockVentas += detInput.CantidadVentas;
                producto.StockInsumos += detInput.CantidadInsumos;
                producto.StockTotal = producto.StockVentas + producto.StockInsumos;
                producto.PrecioCompra = detInput.PrecioUnitario;

                compra.DetalleCompras.Add(detalle);
            }

            compra.Subtotal = subtotal;
            compra.Total = (subtotal + compra.IVA.Value) - compra.Descuento.Value;

            _context.Compras.Add(compra);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<object>.Ok(compra);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail($"Error interno: {ex.InnerException?.Message ?? ex.Message}", 500);
        }
    }

    public async Task<ServiceResult<object>> AnularAsync(int id)
    {
        var compra = await _context.Compras
            .Include(c => c.DetalleCompras)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null) return ServiceResult<object>.NotFound();
        if (compra.Estado == "Anulada") return ServiceResult<object>.Fail("La compra ya esta anulada");

        using var transaction = await _context.Database.BeginTransactionAsync();

        compra.Estado = "Anulada";

        foreach (var detalle in compra.DetalleCompras)
        {
            var productoVal = await _context.Productos.FindAsync(detalle.ProductoId);
            if (productoVal == null) continue;
            if (productoVal.StockVentas < detalle.CantidadVentas || productoVal.StockInsumos < detalle.CantidadInsumos)
            {
                await transaction.RollbackAsync();
                return ServiceResult<object>.Fail($"No se puede anular la compra: el producto '{productoVal.Nombre}' ya tiene stock consumido.");
            }
        }

        foreach (var detalle in compra.DetalleCompras)
        {
            var producto = await _context.Productos.FindAsync(detalle.ProductoId);
            if (producto != null)
            {
                producto.StockVentas -= detalle.CantidadVentas;
                producto.StockInsumos -= detalle.CantidadInsumos;
                producto.StockTotal = producto.StockVentas + producto.StockInsumos;
            }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return ServiceResult<object>.Ok(new { message = "Compra anulada exitosamente" });
    }
}
