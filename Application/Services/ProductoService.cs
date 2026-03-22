using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class ProductoService : IProductoService
{
    private readonly BarberiaContext _context;
    private readonly IPhotoService _photoService;

    public ProductoService(BarberiaContext context, IPhotoService photoService)
    {
        _context = context;
        _photoService = photoService;
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;
        var baseQ = _context.Productos
            .Include(p => p.Categoria)
            .AsNoTracking()
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = $"%{q.Trim()}%";
            baseQ = baseQ.Where(p =>
                (p.Nombre != null && EF.Functions.Like(p.Nombre, term)) ||
                (p.Descripcion != null && EF.Functions.Like(p.Descripcion, term)) ||
                (p.Marca != null && EF.Functions.Like(p.Marca, term)) ||
                (p.Categoria != null && p.Categoria.Nombre != null && EF.Functions.Like(p.Categoria.Nombre, term))
            );
        }
        var totalCount = await baseQ.CountAsync();
        var items = await baseQ
            .OrderBy(p => p.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductoDto
            {
                Id = p.Id, Nombre = p.Nombre, Descripcion = p.Descripcion, Marca = p.Marca, Tipo = p.Tipo,
                PrecioVenta = p.PrecioVenta, PrecioCompra = p.PrecioCompra,
                StockVentas = p.StockVentas, StockInsumos = p.StockInsumos, StockTotal = p.StockTotal, StockMinimo = p.StockMinimo,
                CategoriaId = p.CategoriaId,
                CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
                Estado = p.Estado, ImagenProduc = p.ImagenProduc
            })
            .ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetStockBajoAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;
        var baseQ = _context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.StockTotal <= 5 && p.Estado == true)
            .AsNoTracking()
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(p =>
                (p.Nombre != null && p.Nombre.ToLower().Contains(term)) ||
                (p.Descripcion != null && p.Descripcion.ToLower().Contains(term)) ||
                (p.Marca != null && p.Marca.ToLower().Contains(term)) ||
                (p.Categoria != null && p.Categoria.Nombre != null && p.Categoria.Nombre.ToLower().Contains(term))
            );
        }
        var totalCount = await baseQ.CountAsync();
        var items = await baseQ
            .OrderBy(p => p.StockTotal)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductoDto
            {
                Id = p.Id, Nombre = p.Nombre, Descripcion = p.Descripcion, Marca = p.Marca, Tipo = p.Tipo,
                PrecioVenta = p.PrecioVenta, PrecioCompra = p.PrecioCompra,
                StockVentas = p.StockVentas, StockInsumos = p.StockInsumos, StockTotal = p.StockTotal, StockMinimo = p.StockMinimo,
                CategoriaId = p.CategoriaId,
                CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
                Estado = p.Estado, ImagenProduc = p.ImagenProduc
            })
            .ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var producto = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Where(p => p.Id == id)
            .Select(p => new ProductoDto
            {
                Id = p.Id, Nombre = p.Nombre, Descripcion = p.Descripcion, Marca = p.Marca, Tipo = p.Tipo,
                PrecioVenta = p.PrecioVenta, PrecioCompra = p.PrecioCompra,
                StockVentas = p.StockVentas, StockInsumos = p.StockInsumos, StockTotal = p.StockTotal, StockMinimo = p.StockMinimo,
                CategoriaId = p.CategoriaId,
                CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
                Estado = p.Estado, ImagenProduc = p.ImagenProduc
            })
            .FirstOrDefaultAsync();

        if (producto == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(producto);
    }

    public async Task<ServiceResult<object>> CreateAsync(Producto producto)
    {
        if (producto == null)
            return ServiceResult<object>.Fail("El objeto producto es requerido");

        if (string.IsNullOrWhiteSpace(producto.Nombre))
            return ServiceResult<object>.Fail("El nombre del producto es requerido");

        if (producto.PrecioVenta < 0)
            return ServiceResult<object>.Fail("El precio de venta no puede ser negativo");

        if (!BarberiaApi.Application.Helpers.ValidationHelper.ValidarUrlImagen(producto.ImagenProduc, out var imgError))
        {
            return ServiceResult<object>.Fail(imgError!);
        }

        var nombreExiste = await _context.Productos
            .AnyAsync(p => p.Nombre.ToLower() == producto.Nombre.ToLower() && p.Estado == true);
        if (nombreExiste)
            return ServiceResult<object>.Fail("Ya existe otro producto activo con ese nombre");

        if (producto.CategoriaId.HasValue)
        {
            var categoria = await _context.Categorias.FindAsync(producto.CategoriaId.Value);
            if (categoria == null || categoria.Estado == false)
                return ServiceResult<object>.Fail("La categoría especificada no existe o está inactiva");
        }

        producto.Id = 0;
        producto.Estado = true;
        producto.StockVentas = 0;
        producto.StockInsumos = 0;

        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        return ServiceResult<object>.Ok(producto);
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, Producto producto)
    {
        if (producto == null || id != producto.Id) return ServiceResult<object>.Fail("Payload inválido");

        var productoExistente = await _context.Productos.FindAsync(id);
        if (productoExistente == null) return ServiceResult<object>.NotFound();

        if (string.IsNullOrWhiteSpace(producto.Nombre))
            return ServiceResult<object>.Fail("El nombre del producto es requerido");

        if (producto.PrecioVenta < 0)
            return ServiceResult<object>.Fail("El precio de venta no puede ser negativo");

        if (!BarberiaApi.Application.Helpers.ValidationHelper.ValidarUrlImagen(producto.ImagenProduc, out var imgErrorUpdate))
        {
            return ServiceResult<object>.Fail(imgErrorUpdate!);
        }

        if (!string.IsNullOrWhiteSpace(producto.Nombre) &&
            producto.Nombre.ToLower() != productoExistente.Nombre.ToLower())
        {
            var nombreExiste = await _context.Productos
                .AnyAsync(p => p.Nombre.ToLower() == producto.Nombre.ToLower() &&
                               p.Id != id &&
                               p.Estado == true);
            if (nombreExiste)
                return ServiceResult<object>.Fail("Ya existe otro producto activo con ese nombre");
        }

        if (producto.CategoriaId.HasValue &&
            producto.CategoriaId != productoExistente.CategoriaId)
        {
            var categoria = await _context.Categorias.FindAsync(producto.CategoriaId.Value);
            if (categoria == null || categoria.Estado == false)
                return ServiceResult<object>.Fail("La categoría especificada no existe o está inactiva");
        }

        productoExistente.Nombre = producto.Nombre?.Trim() ?? productoExistente.Nombre;
        productoExistente.Descripcion = producto.Descripcion ?? "";
        productoExistente.Marca = producto.Marca ?? "";
        productoExistente.Tipo = producto.Tipo ?? "";
        productoExistente.PrecioVenta = producto.PrecioVenta;
        productoExistente.PrecioCompra = producto.PrecioCompra;
        productoExistente.StockVentas = producto.StockVentas;
        productoExistente.StockInsumos = producto.StockInsumos;
        productoExistente.StockMinimo = producto.StockMinimo;
        productoExistente.StockTotal = productoExistente.StockVentas + productoExistente.StockInsumos;
        productoExistente.CategoriaId = producto.CategoriaId;
        productoExistente.Estado = producto.Estado;
        productoExistente.ImagenProduc = producto.ImagenProduc ?? productoExistente.ImagenProduc;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Productos.AnyAsync(e => e.Id == id))
                return ServiceResult<object>.NotFound();
            throw;
        }

        return ServiceResult<object>.Ok(new
        {
            productoExistente.Id,
            productoExistente.StockVentas,
            productoExistente.StockInsumos,
            productoExistente.StockTotal
        });
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input)
    {
        var producto = await _context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (producto == null) return ServiceResult<object>.NotFound();

        producto.Estado = input.estado;
        await _context.SaveChangesAsync();

        var response = new CambioEstadoResponse<Producto>
        {
            entidad = producto,
            mensaje = input.estado ? "Producto activado exitosamente" : "Producto desactivado exitosamente",
            exitoso = true
        };

        return ServiceResult<object>.Ok(response);
    }

    public async Task<ServiceResult<object>> TransferirStockAsync(int id, TransferirStockInput input)
    {
        if (input == null)
            return ServiceResult<object>.Fail("Input requerido");

        var producto = await _context.Productos.FindAsync(id);
        if (producto == null) return ServiceResult<object>.NotFound();

        var origen = input.Origen?.ToLower();
        var destino = input.Destino?.ToLower();

        if (origen == destino)
            return ServiceResult<object>.Fail("Origen y destino no pueden ser iguales");

        if (input.Cantidad <= 0)
            return ServiceResult<object>.Fail("La cantidad debe ser mayor a 0");

        if (origen == "ventas")
        {
            if (producto.StockVentas < input.Cantidad)
                return ServiceResult<object>.Fail($"Stock de ventas insuficiente. Disponible: {producto.StockVentas}");
            producto.StockVentas -= input.Cantidad;
            producto.StockInsumos += input.Cantidad;
        }
        else if (origen == "insumos")
        {
            if (producto.StockInsumos < input.Cantidad)
                return ServiceResult<object>.Fail($"Stock de insumos insuficiente. Disponible: {producto.StockInsumos}");
            producto.StockInsumos -= input.Cantidad;
            producto.StockVentas += input.Cantidad;
        }
        else
        {
            return ServiceResult<object>.Fail("Origen debe ser 'ventas' o 'insumos'");
        }

        producto.StockTotal = producto.StockVentas + producto.StockInsumos;
        await _context.SaveChangesAsync();

        return ServiceResult<object>.Ok(new ProductoDto
        {
            Id = producto.Id, Nombre = producto.Nombre, Descripcion = producto.Descripcion, Marca = producto.Marca, Tipo = producto.Tipo,
            PrecioVenta = producto.PrecioVenta, PrecioCompra = producto.PrecioCompra,
            StockVentas = producto.StockVentas, StockInsumos = producto.StockInsumos, StockTotal = producto.StockTotal, StockMinimo = producto.StockMinimo,
            CategoriaId = producto.CategoriaId, Estado = producto.Estado, ImagenProduc = producto.ImagenProduc
        });
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var producto = await _context.Productos
            .Include(p => p.DetalleVenta)
            .Include(p => p.DetalleCompras)
            .Include(p => p.DetalleEntregasInsumos)
            .Include(p => p.Devoluciones)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (producto == null) return ServiceResult<object>.NotFound();

        if (producto.DetalleVenta.Any() ||
            producto.DetalleCompras.Any() ||
            producto.DetalleEntregasInsumos.Any() ||
            producto.Devoluciones.Any())
        {
            producto.Estado = false;
            await _context.SaveChangesAsync();
            return ServiceResult<object>.Ok(new { message = "Producto desactivado (borrado lógico por tener registros asociados)", eliminado = true, fisico = false });
        }

        _context.Productos.Remove(producto);
        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Producto eliminado físicamente de la base de datos", eliminado = true, fisico = true });
    }
}
