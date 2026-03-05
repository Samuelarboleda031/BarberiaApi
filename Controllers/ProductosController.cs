using BarberiaApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberiaApi.Services;
using System;
using System.IO;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly BarberiaContext _context;
        private readonly IPhotoService _photoService;

        public ProductosController(BarberiaContext context, IPhotoService photoService)
        {
            _context = context;
            _photoService = photoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> GetAll()
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .OrderBy(p => p.Nombre)
                .Select(p => new ProductoDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Marca = p.Marca,
                    PrecioVenta = p.PrecioVenta,
                    PrecioCompra = p.PrecioCompra,
                    StockVentas = p.StockVentas,
                    StockInsumos = p.StockInsumos,
                    StockTotal = p.StockTotal,
                    CategoriaId = p.CategoriaId,
                    CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
                    Estado = p.Estado,
                    ImagenProduc = p.ImagenProduc
                })
                .ToListAsync();
        }

        [HttpPost("{id}/imagen")]
        [RequestSizeLimit(15728640)]
        public async Task<ActionResult<object>> SubirImagen(int id, IFormFile imagen)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();
            if (imagen == null || imagen.Length == 0) return BadRequest("Imagen requerida");
            if (string.IsNullOrWhiteSpace(imagen.ContentType) || !imagen.ContentType.StartsWith("image/")) return BadRequest("Content-Type inválido");
            var res = await _photoService.AddPhotoAsync(imagen);
            if (res.Error != null) return BadRequest(res.Error.Message);
            var url = res.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(url)) return BadRequest("Error al subir");
            producto.ImagenProduc = url;
            await _context.SaveChangesAsync();
            return Ok(new { url, publicId = res.PublicId });
        }

        [HttpDelete("{id}/imagen")]
        public async Task<ActionResult<object>> EliminarImagen(int id, [FromQuery] bool borrarCloud = true)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();
            var url = producto.ImagenProduc;
            if (string.IsNullOrWhiteSpace(url)) return Ok(new { eliminado = false });
            string publicId = borrarCloud ? ExtraerPublicIdDesdeUrl(url) ?? "" : "";
            if (borrarCloud && !string.IsNullOrWhiteSpace(publicId)) await _photoService.DeletePhotoAsync(publicId);
            producto.ImagenProduc = null;
            await _context.SaveChangesAsync();
            return Ok(new { eliminado = true, publicId });
        }

        private static string? ExtraerPublicIdDesdeUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var marker = "/upload/";
                var idx = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return null;
                var after = path[(idx + marker.Length)..].Trim('/');
                var segments = after.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 0) return null;
                var start = 0;
                if (segments[0].Length > 1 && segments[0][0] == 'v' && long.TryParse(segments[0][1..], out _)) start = 1;
                if (start >= segments.Length) return null;
                var last = segments[^1];
                var nameNoExt = Path.GetFileNameWithoutExtension(last);
                var leading = segments.Length - start > 1 ? string.Join('/', segments[start..^1]) + "/" : string.Empty;
                return leading + nameNoExt;
            }
            catch
            {
                return null;
            }
        }
        [HttpGet("stock-bajo")]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> GetStockBajo()
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.StockTotal <= 5 && p.Estado == true)
                .OrderBy(p => p.StockTotal)
                .Select(p => new ProductoDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Marca = p.Marca,
                    PrecioVenta = p.PrecioVenta,
                    PrecioCompra = p.PrecioCompra,
                    StockVentas = p.StockVentas,
                    StockInsumos = p.StockInsumos,
                    StockTotal = p.StockTotal,
                    CategoriaId = p.CategoriaId,
                    CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
                    Estado = p.Estado,
                    ImagenProduc = p.ImagenProduc
                })
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoDto>> GetById(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Id == id)
                .Select(p => new ProductoDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Marca = p.Marca,
                    PrecioVenta = p.PrecioVenta,
                    PrecioCompra = p.PrecioCompra,
                    StockVentas = p.StockVentas,
                    StockInsumos = p.StockInsumos,
                    StockTotal = p.StockTotal,
                    CategoriaId = p.CategoriaId,
                    CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
                    Estado = p.Estado,
                    ImagenProduc = p.ImagenProduc
                })
                .FirstOrDefaultAsync();

            if (producto == null) return NotFound();
            return Ok(producto);
        }

        [HttpPost]
        public async Task<ActionResult<Producto>> Create([FromBody] Producto? producto)
        {
            if (producto == null)
                return BadRequest("El objeto producto es requerido");

            if (string.IsNullOrWhiteSpace(producto.Nombre))
                return BadRequest("El nombre del producto es requerido");

            if (producto.PrecioVenta < 0)
                return BadRequest("El precio de venta no puede ser negativo");

            // Validar URL de imagen si se proporciona
            // Validar URL de imagen usando el helper estandarizado
            if (!BarberiaApi.Helpers.ValidationHelper.ValidarUrlImagen(producto.ImagenProduc, out var imgError))
            {
                return BadRequest(imgError);
            }

            // Validar nombre único solo con productos activos
            var nombreExiste = await _context.Productos
                .AnyAsync(p => p.Nombre.ToLower() == producto.Nombre.ToLower() && p.Estado == true);
            if (nombreExiste)
                return BadRequest("Ya existe otro producto activo con ese nombre");

            // Validar categoría si se proporciona
            if (producto.CategoriaId.HasValue)
            {
                var categoria = await _context.Categorias.FindAsync(producto.CategoriaId.Value);
                if (categoria == null || categoria.Estado == false)
                    return BadRequest("La categoría especificada no existe o está inactiva");
            }

            producto.Id = 0;
            producto.Estado = true;
            
            // Garantizar que el stock inicien en 0
            // solo se incrementarán a través de documentos de compra
            producto.StockVentas = 0;
            producto.StockInsumos = 0;
            // Permitir establecer un PrecioCompra inicial si se desea
            // producto.PrecioCompra = 0;

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Producto producto)
        {
            if (id != producto.Id) return BadRequest();

            // Busca el producto existente sin importar el estado
            var productoExistente = await _context.Productos.FindAsync(id);
            // Solo falla si realmente NO existe en la BD
            if (productoExistente == null) return NotFound();

            if (string.IsNullOrWhiteSpace(producto.Nombre))
                return BadRequest("El nombre del producto es requerido");

            if (producto.PrecioVenta < 0)
                return BadRequest("El precio de venta no puede ser negativo");

            // Validar URL de imagen si se proporciona
            // Validar URL de imagen usando el helper estandarizado
            if (!BarberiaApi.Helpers.ValidationHelper.ValidarUrlImagen(producto.ImagenProduc, out var imgErrorUpdate))
            {
                return BadRequest(imgErrorUpdate);
            }

            // Validar nombre único solo con productos activos (si el nombre cambió)
            if (!string.IsNullOrWhiteSpace(producto.Nombre) && 
                producto.Nombre.ToLower() != productoExistente.Nombre.ToLower())
            {
                var nombreExiste = await _context.Productos
                    .AnyAsync(p => p.Nombre.ToLower() == producto.Nombre.ToLower() && 
                                   p.Id != id && 
                                   p.Estado == true);
                if (nombreExiste)
                    return BadRequest("Ya existe otro producto activo con ese nombre");
            }

            // Validar categoría si cambió
            if (producto.CategoriaId.HasValue && 
                producto.CategoriaId != productoExistente.CategoriaId)
            {
                var categoria = await _context.Categorias.FindAsync(producto.CategoriaId.Value);
                if (categoria == null || categoria.Estado == false)
                    return BadRequest("La categoría especificada no existe o está inactiva");
            }

            // Conservar valores que NO deben editarse manualmente (solo por documentos de compra/venta)
            var stockVentasOriginal = productoExistente.StockVentas;
            var stockInsumosOriginal = productoExistente.StockInsumos;
            
            _context.Entry(productoExistente).CurrentValues.SetValues(producto);

            // Restaurar los valores protegidos
            productoExistente.StockVentas = stockVentasOriginal;
            productoExistente.StockInsumos = stockInsumosOriginal;
            // Permitir actualizar PrecioCompra manualmente si se desea
            // productoExistente.PrecioCompra = precioCompraOriginal;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Productos.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpPut("{id}/estado")]
        public async Task<ActionResult<CambioEstadoResponse<Producto>>> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        {
            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null) return NotFound();

            // Actualizar solo el estado
            producto.Estado = input.estado;
            await _context.SaveChangesAsync();

            var response = new CambioEstadoResponse<Producto>
            {
                entidad = producto,
                mensaje = input.estado ? "Producto activado exitosamente" : "Producto desactivado exitosamente",
                exitoso = true
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Busca el producto sin importar el estado
            var producto = await _context.Productos
                .Include(p => p.DetalleVenta)
                .Include(p => p.DetalleCompras)
                .Include(p => p.DetalleEntregasInsumos)
                .Include(p => p.Devoluciones)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            // Solo falla si realmente NO existe en la BD
            if (producto == null) return NotFound();

            // Verificar si tiene ventas, compras, entregas o devoluciones
            if (producto.DetalleVenta.Any() || 
                producto.DetalleCompras.Any() || 
                producto.DetalleEntregasInsumos.Any() || 
                producto.Devoluciones.Any())
            {
                // Soft delete: Cambia el estado a false
                producto.Estado = false;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Producto desactivado (borrado lógico por tener registros asociados)", eliminado = true, fisico = false });
            }

            // Borrado Físico: No tiene registros asociados
            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Producto eliminado físicamente de la base de datos", eliminado = true, fisico = true });
        }
    }
}
