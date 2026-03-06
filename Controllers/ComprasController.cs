using BarberiaApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComprasController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public ComprasController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Compra>>> GetAll()
        {
            return await _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Usuario)
                .OrderByDescending(c => c.FechaRegistro)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Compra>> GetById(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Usuario)
                .Include(c => c.DetalleCompras)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (compra == null) return NotFound();
            return Ok(compra);
        }

        [HttpPost]
        public async Task<ActionResult<Compra>> Create([FromBody] CompraInput input)
        {
            if (input == null || input.Detalles == null || !input.Detalles.Any())
                return BadRequest("La compra debe tener al menos un detalle");
            var proveedorVal = await _context.Proveedores.FindAsync(input.ProveedorId);
            if (proveedorVal == null)
                return BadRequest("El proveedor no existe");
            if (proveedorVal.Estado.HasValue && !proveedorVal.Estado.Value)
                return BadRequest("El proveedor está inactivo");
            var usuarioVal = await _context.Usuarios.FindAsync(input.UsuarioId);
            if (usuarioVal == null)
                return BadRequest("El usuario no existe");

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
                        return BadRequest($"El producto {detInput.ProductoId} no existe");
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

                return CreatedAtAction(nameof(GetById), new { id = compra.Id }, compra);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                
                var innerException = ex.InnerException;
                var errorMessage = innerException?.Message ?? ex.Message;
                var fullError = $"Error interno: {errorMessage}\n\nInnerException Full: {innerException?.ToString() ?? "N/A"}\n\nMain Exception: {ex.ToString()}";
                
                return StatusCode(500, fullError);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.DetalleCompras)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (compra == null) return NotFound();

            if (compra.Estado == "Anulada")
                return BadRequest("La compra ya está anulada");

            using var transaction = await _context.Database.BeginTransactionAsync();
            compra.Estado = "Anulada";

            // Validación previa: asegurar stock suficiente para revertir la compra
            foreach (var detalle in compra.DetalleCompras)
            {
                var productoVal = await _context.Productos.FindAsync(detalle.ProductoId);
                if (productoVal == null)
                    continue;
                if (productoVal.StockVentas < detalle.CantidadVentas || productoVal.StockInsumos < detalle.CantidadInsumos)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"No se puede anular la compra: el producto '{productoVal.Nombre}' ya tiene stock consumido. Ventas requeridas a revertir: {detalle.CantidadVentas}, Insumos requeridos a revertir: {detalle.CantidadInsumos}. Stock actual Ventas: {productoVal.StockVentas}, Insumos: {productoVal.StockInsumos}");
                }
            }

            foreach (var detalle in compra.DetalleCompras)
            {
                var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                if (producto != null)
                {
                    var nuevoVentas = producto.StockVentas - detalle.CantidadVentas;
                    var nuevoInsumos = producto.StockInsumos - detalle.CantidadInsumos;
                    producto.StockVentas = nuevoVentas;
                    producto.StockInsumos = nuevoInsumos;
                    producto.StockTotal = producto.StockVentas + producto.StockInsumos;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return NoContent();
        }
    }
}
