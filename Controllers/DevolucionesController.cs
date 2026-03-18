    using BarberiaApi.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Data;
    using Microsoft.AspNetCore.OutputCaching;

    namespace BarberiaApi.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        public class DevolucionesController : ControllerBase
        {
            private readonly BarberiaContext _context;
            private static bool IsErrorCompra(string? categoria, string? detalle)
            {
                var cat = (categoria ?? string.Empty).Trim().ToLower();
                var normalized = cat.Replace("_", "").Replace("-", "").Replace(" ", "");
                if (normalized == "errorcompra" || normalized == "errorenlacompra") return true;
                var det = (detalle ?? string.Empty).Trim().ToLower();
                return det.Contains("error") && det.Contains("compra");
            }

            public DevolucionesController(BarberiaContext context)
            {
                _context = context;
            }

            // ================================
            // DEVOLUCIÓN DE INSUMOS A BARBERO
            // ================================
            // Registra una devolución de insumos desde un barbero hacia el inventario.
            // No genera saldo a favor ni afecta ventas.
            [HttpPost("insumos/barbero")]
            public async Task<ActionResult<object>> DevolucionInsumosBarbero([FromBody] EntregaInput input)
            {
                if (input == null || input.Detalles == null || !input.Detalles.Any())
                    return BadRequest("La devolución debe tener al menos un detalle");

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

                    bool esErrorCompraInsumos = IsErrorCompra(input.MotivoCategoria, input.MotivoDetalle);

                    foreach (var det in input.Detalles)
                    {
                        var producto = await _context.Productos.FindAsync(det.ProductoId);
                        if (producto == null)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest($"Producto {det.ProductoId} no encontrado");
                        }

                        var entregado = await _context.DetalleEntregasInsumos
                            .Where(d => d.ProductoId == det.ProductoId &&
                                        d.Entrega.BarberoId == input.BarberoId &&
                                        d.Entrega.Estado == "Entregado")
                            .SumAsync(d => (int?)d.Cantidad) ?? 0;

                        var devuelto = await _context.Devoluciones
                            .Where(d => d.ProductoId == det.ProductoId &&
                                        d.BarberoId == input.BarberoId &&
                                        d.Estado == "Activo")
                            .SumAsync(d => (int?)d.Cantidad) ?? 0;

                        var disponible = entregado - devuelto;
                        if (det.Cantidad > disponible)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest($"Cantidad a devolver para el producto {producto.Nombre} ({det.Cantidad}) excede lo entregado al barbero ({disponible}).");
                        }

                        // Devolver al stock de insumos SOLO si es error en la compra
                        if (esErrorCompraInsumos)
                        {
                            producto.StockInsumos += det.Cantidad;
                            producto.StockTotal = producto.StockVentas + producto.StockInsumos;
                        }

                        var detalle = new DetalleEntregasInsumo
                        {
                            ProductoId = det.ProductoId,
                            Cantidad = det.Cantidad,
                            // Guardamos el precio histórico de referencia (venta) si está disponible
                            PrecioHistorico = det.PrecioHistorico ?? producto.PrecioVenta
                        };

                        cantidadTotal += det.Cantidad;
                        valorTotal += (detalle.PrecioHistorico ?? 0) * det.Cantidad;

                        entrega.DetalleEntregasInsumos.Add(detalle);
                    }

                    entrega.CantidadTotal = cantidadTotal;
                    entrega.ValorTotal = valorTotal;

                    _context.EntregasInsumos.Add(entrega);
                    await _context.SaveChangesAsync();
                    
                    foreach (var det in input.Detalles)
                    {
                        var dev = new Devolucion
                        {
                            VentaId = null,
                            ClienteId = null,
                            UsuarioId = input.UsuarioId,
                            BarberoId = input.BarberoId,
                            EntregaId = entrega.Id,
                            ProductoId = det.ProductoId,
                            Cantidad = det.Cantidad,
                            MotivoCategoria = input.MotivoCategoria ?? "Insumos",
                            MotivoDetalle = input.MotivoDetalle,
                            Observaciones = null,
                            MontoDevuelto = 0,
                            SaldoAFavor = 0,
                            Fecha = DateTime.Now,
                            Estado = "Activo"
                        };
                        _context.Devoluciones.Add(dev);
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return CreatedAtAction("GetById", new { id = entrega.Id }, new
                    {
                        entrega.Id,
                        entrega.BarberoId,
                        entrega.UsuarioId,
                        entrega.CantidadTotal,
                        entrega.ValorTotal,
                        entrega.Estado
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Error interno: {ex.Message}");
                }
            }

            [HttpGet]
            [OutputCache(PolicyName = "short")]
            public async Task<ActionResult<object>> GetAll([FromQuery] int? barberoId, [FromQuery] int? clienteId, [FromQuery] int? productoId, [FromQuery] int? entregaId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
            {
                var query = _context.Devoluciones
                    .AsNoTracking()
                    .AsQueryable();

                if (barberoId.HasValue) query = query.Where(d => d.BarberoId == barberoId.Value);
                if (clienteId.HasValue) query = query.Where(d => d.ClienteId == clienteId.Value);
                if (productoId.HasValue) query = query.Where(d => d.ProductoId == productoId.Value);
                if (entregaId.HasValue) query = query.Where(d => d.EntregaId == entregaId.Value);
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
                        )) ||
                        (d.Barbero != null && d.Barbero.Usuario != null && (
                            (d.Barbero.Usuario.Nombre != null && d.Barbero.Usuario.Nombre.ToLower().Contains(term)) ||
                            (d.Barbero.Usuario.Apellido != null && d.Barbero.Usuario.Apellido.ToLower().Contains(term))
                        ))
                    );
                }

                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 5;
                var queryOrdered = query.OrderByDescending(d => d.Fecha);
                var totalCount = await queryOrdered.CountAsync();
                var items = await queryOrdered
                        .Select(d => new
                        {
                            d.Id,
                            d.VentaId,
                            d.EntregaId,
                            d.ClienteId,
                            d.UsuarioId,
                            d.BarberoId,
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
                                : null,
                            Barbero = d.BarberoId.HasValue && d.Barbero != null && d.Barbero.Usuario != null
                                ? new { d.Barbero.Id, Nombre = d.Barbero.Usuario.Nombre }
                                : null,
                            Entrega = d.EntregaId.HasValue && d.Entrega != null
                                ? new { d.Entrega.Id, d.Entrega.Estado, d.Entrega.Fecha }
                                : null
                        })
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                return Ok(new { items, totalCount, page, pageSize, totalPages });
            }

            [HttpGet("{id}")]
            public async Task<ActionResult<object>> GetById(int id)
            {
                var devolucion = await _context.Devoluciones
                    .Where(d => d.Id == id)
                    .Select(d => new
                    {
                        d.Id,
                        d.VentaId,
                        d.EntregaId,
                        d.ClienteId,
                        d.UsuarioId,
                        d.BarberoId,
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
                            : null,
                        Barbero = d.BarberoId.HasValue && d.Barbero != null && d.Barbero.Usuario != null
                            ? new { d.Barbero.Id, Nombre = d.Barbero.Usuario.Nombre }
                            : null,
                        Entrega = d.EntregaId.HasValue && d.Entrega != null
                            ? new { d.Entrega.Id, d.Entrega.Estado, d.Entrega.Fecha }
                            : null
                    })
                    .FirstOrDefaultAsync();

                if (devolucion == null) return NotFound();
                return Ok(devolucion);
            }

            [HttpPost]
            public async Task<ActionResult<Devolucion>> Create([FromBody] DevolucionInput input)
            {
                if (input == null) return BadRequest();

                using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                try
                {
                    if (!input.VentaId.HasValue)
                        return BadRequest("VentaId es requerido para devoluciones de ventas");
                    
                    if (input.BarberoId.HasValue || input.EntregaId.HasValue)
                        return BadRequest("Una devolución de ventas no puede tener BarberoId ni EntregaId");
                    
                    var venta = await _context.Ventas
                        .FirstOrDefaultAsync(v => v.Id == input.VentaId.Value);

                    if (venta == null)
                        return BadRequest("La venta no existe");

                    var fechaVenta = venta.Fecha ?? DateTime.Now;
                    var exp = fechaVenta.AddDays(15);
                    if (DateTime.Now > exp)
                        return BadRequest("Garantía expirada para esta venta");

                    var vendido = await _context.DetalleVentas
                        .Where(d => d.VentaId == input.VentaId.Value && d.ProductoId == input.ProductoId)
                        .SumAsync(d => (int?)d.Cantidad) ?? 0;
                    if (vendido <= 0)
                        return BadRequest("El producto indicado no figura como vendido en esta venta.");
                    
                    var yaDevuelto = await _context.Devoluciones
                        .Where(d => d.VentaId == input.VentaId.Value && d.ProductoId == input.ProductoId && d.Estado != "Anulado")
                        .SumAsync(d => (int?)d.Cantidad) ?? 0;
                    var disponible = vendido - yaDevuelto;
                    if (disponible <= 0)
                        return BadRequest("No hay cantidad disponible para devolver de este producto en la venta seleccionada.");
                    if (input.Cantidad > disponible)
                        return BadRequest($"Cantidad a devolver ({input.Cantidad}) excede disponible ({disponible}). Vendidos: {vendido}, ya devueltos: {yaDevuelto}.");

                    bool esErrorCompraVenta = IsErrorCompra(input.MotivoCategoria, input.MotivoDetalle);

                    var devolucion = new Devolucion
                    {
                        VentaId = input.VentaId,
                        ClienteId = input.ClienteId ?? venta.ClienteId,
                        UsuarioId = input.UsuarioId,
                        BarberoId = null,
                        EntregaId = null,
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
                            producto.StockVentas += input.Cantidad;
                            producto.StockTotal = producto.StockVentas + producto.StockInsumos;
                        }
                    }

                    _context.Devoluciones.Add(devolucion);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return CreatedAtAction(nameof(GetById), new { id = devolucion.Id }, devolucion);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Error interno: {ex.Message}");
                }
            }

            [HttpPost("lote")]
            public async Task<ActionResult> CreateBatch([FromBody] DevolucionBatchInput input)
            {
                if (input == null || input.Items == null || input.Items.Count == 0)
                    return BadRequest("No hay items para registrar");

                using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                try
                {
                    var venta = await _context.Ventas.FindAsync(input.VentaId);
                    if (venta == null) return BadRequest("La venta no existe");

                    if (input.BarberoId.HasValue)
                        return BadRequest("Una devolución de ventas por lote no puede tener BarberoId ni EntregaId");

                    var clienteId = input.ClienteId ?? venta.ClienteId;
                    if (clienteId == null) return BadRequest("Venta no asociada a cliente");

                    var fechaVenta = venta.Fecha ?? DateTime.Now;
                    var exp = fechaVenta.AddDays(15);
                    if (DateTime.Now > exp) return BadRequest("Garantía expirada para esta venta");

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
                            return BadRequest($"El producto {pid} no figura como vendido en esta venta.");
                        var yaDev = yaDevueltosPorProducto.ContainsKey(pid) ? yaDevueltosPorProducto[pid] : 0;
                        var disponible = vendidos - yaDev;
                        if (disponible <= 0)
                            return BadRequest($"No hay cantidad disponible para devolver del producto {pid} en esta venta.");
                        if (propuesto > disponible)
                            return BadRequest($"Cantidad propuesta a devolver para producto {pid} ({propuesto}) excede disponible ({disponible}). Vendidos: {vendidos}, ya devueltos: {yaDev}.");
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
                            MotivoDetalle = input.MotivoCategoria,
                            Observaciones = input.Observaciones,
                            MontoDevuelto = it.MontoDevuelto,
                            SaldoAFavor = it.MontoDevuelto,
                            Fecha = DateTime.Now,
                            Estado = "Activo"
                        };

                        _context.Devoluciones.Add(dev);

                        if (esErrorCompraBatch)
                        {
                            var p = await _context.Productos.FindAsync(it.ProductoId);
                            if (p != null)
                            {
                                p.StockVentas += it.Cantidad;
                                p.StockTotal = p.StockVentas + p.StockInsumos;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                    return Ok(new { exitoso = true });
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    return StatusCode(500, ex.Message);
                }
            }

            [HttpPost("{id}/estado")]
            public async Task<ActionResult<object>> CambiarEstado(int id, [FromBody] CambioEstadoInput input)
            {
                if (input == null || string.IsNullOrWhiteSpace(input.estado))
                    return BadRequest("El estado es requerido");
                
                // Validar estados permitidos
                var estadosValidos = new[] { "Activo", "Anulado", "Pendiente", "Procesado" };
                if (!estadosValidos.Contains(input.estado, StringComparer.OrdinalIgnoreCase))
                    return BadRequest($"Estado '{input.estado}' no es válido. Estados permitidos: {string.Join(", ", estadosValidos)}");
                
                try 
                {
                    var devolucion = await _context.Devoluciones
                        .Include(d => d.Producto)
                        .FirstOrDefaultAsync(d => d.Id == id);
                    if (devolucion == null)
                        return NotFound();
                    
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
                                if (devolucion.Producto.StockVentas >= devolucion.Cantidad)
                                {
                                    devolucion.Producto.StockVentas -= devolucion.Cantidad;
                                    devolucion.Producto.StockTotal = devolucion.Producto.StockVentas + devolucion.Producto.StockInsumos;
                                }
                                else
                                {
                                    return BadRequest($"Stock insuficiente. Stock actual: {devolucion.Producto.StockVentas}, requerido: {devolucion.Cantidad}");
                                }
                            }
                            else if (esReactivacion)
                            {
                                devolucion.Producto.StockVentas += devolucion.Cantidad;
                                devolucion.Producto.StockTotal = devolucion.Producto.StockVentas + devolucion.Producto.StockInsumos;
                            }
                        }
                        else if (esErrorCompra && devolucion.BarberoId.HasValue)
                        {
                            if (esAnulacion)
                            {
                                if (devolucion.Producto.StockInsumos >= devolucion.Cantidad)
                                {
                                    devolucion.Producto.StockInsumos -= devolucion.Cantidad;
                                    devolucion.Producto.StockTotal = devolucion.Producto.StockVentas + devolucion.Producto.StockInsumos;
                                }
                                else
                                {
                                    return BadRequest($"Stock insuficiente. Stock actual insumos: {devolucion.Producto.StockInsumos}, requerido: {devolucion.Cantidad}");
                                }
                            }
                            else if (esReactivacion)
                            {
                                devolucion.Producto.StockInsumos += devolucion.Cantidad;
                                devolucion.Producto.StockTotal = devolucion.Producto.StockVentas + devolucion.Producto.StockInsumos;
                            }
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    return Ok(new
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
                    return StatusCode(500, $"Error interno al cambiar estado: {ex.Message}");
                }
            }

            [HttpPut("{id}")]
            public async Task<IActionResult> Update(int id, [FromBody] DevolucionUpdateInput input)
            {
                if (id != input.Id) return BadRequest();

                var existente = await _context.Devoluciones.FindAsync(id);
                if (existente == null) return NotFound();

                if (input.VentaId.HasValue)
                {
                    if (input.BarberoId.HasValue || input.EntregaId.HasValue)
                        return BadRequest("Una devolución de ventas no puede tener BarberoId ni EntregaId");
                    
                    existente.VentaId = input.VentaId;
                    existente.ClienteId = input.ClienteId;
                    existente.UsuarioId = input.UsuarioId;
                    existente.BarberoId = null;
                    existente.EntregaId = null;
                    existente.ProductoId = input.ProductoId;
                    existente.Cantidad = input.Cantidad;
                    existente.MotivoCategoria = input.MotivoCategoria;
                    existente.MotivoDetalle = input.MotivoDetalle;
                    existente.Observaciones = input.Observaciones;
                    existente.MontoDevuelto = input.MontoDevuelto;
                    existente.SaldoAFavor = input.SaldoAFavor;
                    existente.Estado = input.Estado;
                }
                else
                {
                    if (!input.BarberoId.HasValue || !input.EntregaId.HasValue)
                        return BadRequest("Una devolución de insumos requiere BarberoId y EntregaId, y no debe tener VentaId");
                    
                    existente.VentaId = null;
                    existente.ClienteId = null;
                    existente.UsuarioId = input.UsuarioId;
                    existente.BarberoId = input.BarberoId;
                    existente.EntregaId = input.EntregaId;
                    existente.ProductoId = input.ProductoId;
                    existente.Cantidad = input.Cantidad;
                    existente.MotivoCategoria = input.MotivoCategoria;
                    existente.MotivoDetalle = input.MotivoDetalle;
                    existente.Observaciones = input.Observaciones;
                    existente.MontoDevuelto = 0;
                    existente.SaldoAFavor = 0;
                    existente.Estado = input.Estado;
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
    }
}
