using BarberiaApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public VentasController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Venta>>> GetAll()
        {
            return await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.Barbero)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Venta>> GetById(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.Barbero)
                .Include(v => v.DetalleVenta)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.DetalleVenta)
                    .ThenInclude(d => d.Servicio)
                .Include(v => v.DetalleVenta)
                    .ThenInclude(d => d.Paquete)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null) return NotFound();
            return Ok(venta);
        }

        [HttpPost]
        public async Task<ActionResult<Venta>> Create([FromBody] VentaInput input)
        {
            if (input == null || input.Detalles == null || !input.Detalles.Any())
                return BadRequest("La venta debe tener al menos un detalle");

            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                bool tieneServicioDirecto = input.Detalles.Any(d => d.ServicioId.HasValue);
                bool paqueteConServicio = false;
                var paqueteIds = input.Detalles.Where(d => d.PaqueteId.HasValue).Select(d => d.PaqueteId!.Value).Distinct().ToList();
                if (paqueteIds.Count > 0)
                {
                    paqueteConServicio = await _context.DetallePaquetes.AnyAsync(dp => paqueteIds.Contains(dp.PaqueteId) && dp.ServicioId != null);
                }
                bool requiereBarbero = tieneServicioDirecto || paqueteConServicio;
                if (requiereBarbero && !input.BarberoId.HasValue)
                    return BadRequest("Se requiere BarberoId cuando la venta incluye servicios");

                int usuarioId = input.UsuarioId;
                if (requiereBarbero)
                {
                    var barberoExistente = await _context.Barberos.FirstOrDefaultAsync(b => b.Id == input.BarberoId!.Value);
                    if (barberoExistente == null)
                        return BadRequest("El barbero especificado no existe");
                }

                if (usuarioId == 0 && input.BarberoId.HasValue)
                {
                    var barbero = await _context.Barberos.FirstOrDefaultAsync(b => b.Id == input.BarberoId.Value);
                    if (barbero == null)
                    {
                        return BadRequest("El barbero especificado no existe");
                    }
                    usuarioId = barbero.UsuarioId;
                    if (usuarioId == 0)
                    {
                        return BadRequest("El barbero especificado no tiene usuario asociado válido");
                    }
                }
                else
                {
                    var usuario = await _context.Usuarios.FindAsync(usuarioId);
                    if (usuario == null)
                        return BadRequest("El usuario especificado no existe");
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

                    // Calcular subtotal del detalle
                    detalle.Subtotal = detalle.Cantidad * detalle.PrecioUnitario;
                    subtotal += detalle.Subtotal;

                    // Manejo de Stock si es producto
                    if (detInput.ProductoId.HasValue)
                    {
                        var producto = await _context.Productos.FindAsync(detInput.ProductoId.Value);
                        if (producto == null) return BadRequest($"Producto {detInput.ProductoId} no encontrado");
                        
                        if (producto.StockVentas < detInput.Cantidad)
                            return BadRequest($"Stock insuficiente para el producto {producto.Nombre}");

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
                    {
                        aplicable = Math.Min(solicitado, Math.Min(disponible, venta.Total));
                    }
                    else if (input.UsarSaldoAFavor == true || input.SaldoAFavorUsado == null)
                    {
                        aplicable = Math.Min(disponible, venta.Total);
                    }
                    
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
                    .Include(v => v.Cliente)
                    .Include(v => v.Usuario)
                    .Include(v => v.Barbero)
                    .Include(v => v.DetalleVenta)
                        .ThenInclude(d => d.Producto)
                    .Include(v => v.DetalleVenta)
                        .ThenInclude(d => d.Servicio)
                    .Include(v => v.DetalleVenta)
                        .ThenInclude(d => d.Paquete)
                    .FirstOrDefaultAsync(v => v.Id == venta.Id);

                return CreatedAtAction(nameof(GetById), new { id = venta.Id }, ventaCompleta);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpPut("{id}/anular")]
        public async Task<ActionResult<CambioEstadoResponse<Venta>>> AnularVenta(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.DetalleVenta)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null) return NotFound();

            if (venta.Estado == "Anulada")
                return BadRequest("La venta ya está anulada");

            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var estadoAnterior = venta.Estado;
                venta.Estado = "Anulada";

                if ((venta.SaldoAFavorUsado ?? 0) > 0)
                    venta.SaldoAFavorUsado = 0;

                // Revertir stock de productos
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

                var response = new CambioEstadoResponse<Venta>
                {
                    entidad = venta,
                    mensaje = $"Venta anulada exitosamente. Estado anterior: '{estadoAnterior}'",
                    exitoso = true
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}
