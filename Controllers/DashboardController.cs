using BarberiaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public DashboardController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<object>> Get()
        {
            var hoy = DateTime.Today;
            var desdeVentas = hoy.AddDays(-365);
            var limiteAgendas = DateTime.Now.AddDays(-7);

            var ventas = await _context.Ventas
                .Where(v => v.Fecha >= desdeVentas)
                .Include(v => v.Cliente)
                    .ThenInclude(c => c.Usuario)
                .Include(v => v.DetalleVenta)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.DetalleVenta)
                    .ThenInclude(d => d.Servicio)
                .Include(v => v.DetalleVenta)
                    .ThenInclude(d => d.Paquete)
                .OrderByDescending(v => v.Fecha)
                .Select(v => new
                {
                    id = v.Id,
                    fecha = v.Fecha,
                    estado = v.Estado,
                    total = v.Total,
                    clienteId = v.ClienteId,
                    cliente = v.Cliente != null && v.Cliente.Usuario != null
                        ? (v.Cliente.Usuario.Nombre + " " + v.Cliente.Usuario.Apellido)
                        : null,
                    productosDetalle = v.DetalleVenta
                        .Where(d => d.ProductoId != null)
                        .Select(d => new
                        {
                            nombre = d.Producto != null ? d.Producto.Nombre : "Producto",
                            cantidad = d.Cantidad,
                            precio = d.PrecioUnitario
                        }),
                    serviciosDetalle = v.DetalleVenta
                        .Where(d => d.ServicioId != null || d.PaqueteId != null)
                        .Select(d => new
                        {
                            nombre = d.Servicio != null
                                ? d.Servicio.Nombre
                                : (d.Paquete != null ? d.Paquete.Nombre : "Servicio"),
                            cantidad = d.Cantidad,
                            precio = d.PrecioUnitario
                        })
                })
                .ToListAsync();

            var agendamientos = await _context.Agendamientos
                .Where(a => a.FechaHora >= limiteAgendas)
                .Include(a => a.Cliente)
                    .ThenInclude(c => c.Usuario)
                .Include(a => a.Barbero)
                    .ThenInclude(b => b.Usuario)
                .Include(a => a.Servicio)
                .Include(a => a.Paquete)
                .OrderByDescending(a => a.FechaHora)
                .Select(a => new
                {
                    id = a.Id,
                    clienteNombre = a.Cliente.Usuario.Nombre + " " + a.Cliente.Usuario.Apellido,
                    barberoNombre = a.Barbero.Usuario.Nombre + " " + a.Barbero.Usuario.Apellido,
                    servicioNombre = a.Servicio != null ? a.Servicio.Nombre : null,
                    paqueteNombre = a.Paquete != null ? a.Paquete.Nombre : null,
                    fechaHora = a.FechaHora,
                    estado = a.Estado,
                    duracion = a.Duracion,
                    precio = a.Precio,
                    notas = a.Notas
                })
                .ToListAsync();

            var inventarioBajo = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.StockTotal <= 5 && p.Estado == true)
                .OrderBy(p => p.StockTotal)
                .Select(p => new
                {
                    nombre = p.Nombre,
                    stockVentas = p.StockVentas,
                    stockInsumos = p.StockInsumos,
                    stockTotal = p.StockTotal,
                    minimo = 5,
                    categoria = p.Categoria != null ? p.Categoria.Nombre : null
                })
                .ToListAsync();

            var data = new
            {
                ventas,
                agendamientos,
                inventarioBajo
            };

            return Ok(data);
        }
    }
}
