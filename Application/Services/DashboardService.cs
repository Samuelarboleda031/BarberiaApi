using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly BarberiaContext _context;
    public DashboardService(BarberiaContext context) => _context = context;

    public async Task<ServiceResult<object>> GetDashboardAsync()
    {
        var hoy = DateTime.Today;
        var desdeVentas = hoy.AddDays(-365);
        var limiteAgendas = DateTime.Now.AddDays(-7);

        // Obtener solo las ventas de los últimos 30 días activas
        var ventasRecientes = await _context.Ventas.AsNoTracking().AsSplitQuery()
            .Where(v => v.Fecha >= hoy.AddDays(-30) && v.Estado != "Anulada" && v.Estado != "Cancelada")
            .Include(v => v.Cliente).ThenInclude(c => c.Usuario)
            .Include(v => v.Barbero).ThenInclude(b => b.Usuario)
            .Include(v => v.DetalleVenta).ThenInclude(d => d.Producto)
            .Include(v => v.DetalleVenta).ThenInclude(d => d.Servicio)
            .Include(v => v.DetalleVenta).ThenInclude(d => d.Paquete)
            .OrderByDescending(v => v.Fecha)
            .Select(v => new {
                id = v.Id, fecha = v.Fecha, estado = v.Estado, total = v.Total, clienteId = v.ClienteId,
                cliente = v.Cliente != null && v.Cliente.Usuario != null ? (v.Cliente.Usuario.Nombre + " " + v.Cliente.Usuario.Apellido) : (string?)null,
                barberoId = v.BarberoId,
                barbero = v.Barbero != null && v.Barbero.Usuario != null ? (v.Barbero.Usuario.Nombre + " " + v.Barbero.Usuario.Apellido) : (string?)null,
                productosDetalle = v.DetalleVenta.Where(d => d.ProductoId != null).Select(d => new { nombre = d.Producto != null ? d.Producto.Nombre : "Producto", cantidad = d.Cantidad, precio = d.PrecioUnitario }),
                serviciosDetalle = v.DetalleVenta.Where(d => d.ServicioId != null || d.PaqueteId != null).Select(d => new {
                    nombre = d.Servicio != null ? d.Servicio.Nombre : (d.Paquete != null ? d.Paquete.Nombre : "Servicio"),
                    tipo = d.PaqueteId != null ? "Paquete" : "Servicio", cantidad = d.Cantidad, precio = d.PrecioUnitario })
            }).ToListAsync();

        // Para el histórico anual, enviamos totales desglosados solo de ventas activas
        var ventasHistoricas = await _context.Ventas.AsNoTracking()
            .Where(v => v.Fecha >= desdeVentas && v.Fecha < hoy.AddDays(-30) && v.Estado != "Anulada" && v.Estado != "Cancelada")
            .Include(v => v.Barbero).ThenInclude(b => b.Usuario)
            .Select(v => new { 
                fecha = v.Fecha, 
                total = v.Total, 
                estado = v.Estado,
                barberoId = v.BarberoId,
                barbero = v.Barbero != null && v.Barbero.Usuario != null ? (v.Barbero.Usuario.Nombre + " " + v.Barbero.Usuario.Apellido) : (string?)null,
                totalProductos = v.DetalleVenta.Where(d => d.ProductoId != null).Sum(d => (decimal?)d.PrecioUnitario * d.Cantidad) ?? 0,
                totalServicios = v.DetalleVenta.Where(d => d.ServicioId != null || d.PaqueteId != null).Sum(d => (decimal?)d.PrecioUnitario * d.Cantidad) ?? 0
            })
            .ToListAsync();

        var agendamientos = await _context.Agendamientos.AsNoTracking().AsSplitQuery()
            .Where(a => a.FechaHora >= limiteAgendas)
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.Servicio).Include(a => a.Paquete)
            .OrderByDescending(a => a.FechaHora)
            .Select(a => new {
                id = a.Id, clienteNombre = a.Cliente.Usuario.Nombre + " " + a.Cliente.Usuario.Apellido,
                barberoNombre = a.Barbero.Usuario.Nombre + " " + a.Barbero.Usuario.Apellido,
                servicioNombre = a.Servicio != null ? a.Servicio.Nombre : (string?)null,
                paqueteNombre = a.Paquete != null ? a.Paquete.Nombre : (string?)null,
                fechaHora = a.FechaHora, estado = a.Estado, duracion = a.Duracion, precio = a.Precio, notas = a.Notas
            }).ToListAsync();

        var inventarioBajo = await _context.Productos.AsNoTracking().Include(p => p.Categoria)
            .Where(p => (p.StockVentas + p.StockInsumos) < 50 && p.Estado == true).OrderBy(p => (p.StockVentas + p.StockInsumos))
            .Select(p => new { nombre = p.Nombre, stockVentas = p.StockVentas, stockInsumos = p.StockInsumos, stockTotal = (p.StockVentas + p.StockInsumos), minimo = 50,
                categoria = p.Categoria != null ? p.Categoria.Nombre : (string?)null }).ToListAsync();

        return ServiceResult<object>.Ok(new { 
            ventas = ventasRecientes, 
            ventasHistoricas,
            agendamientos, 
            inventarioBajo 
        });
    }

    public async Task<ServiceResult<object>> GetGananciasAsync(string periodo, string barbero)
    {
        var hoy = DateTime.Now.Date;
        
        int dias = periodo.ToLower() switch
        {
            "hoy" => 1,
            "semanal" => 7,
            "mensual" => 30,
            "anual" => 365,
            _ => 1
        };

        var fechaInicio = hoy.AddDays(-(dias - 1));

        var ventasQuery = await _context.Ventas.AsNoTracking()
            .Include(v => v.Barbero).ThenInclude(b => b.Usuario)
            .Include(v => v.DetalleVenta)
            .Where(v => v.Fecha >= fechaInicio &&
                        v.Estado != "Anulada" &&
                        v.Estado != "Cancelada")
            .Select(v => new {
                BarberoNombre = v.Barbero != null && v.Barbero.Usuario != null ? (v.Barbero.Usuario.Nombre + " " + v.Barbero.Usuario.Apellido) : "",
                TotalServicios = v.DetalleVenta
                    .Where(d => d.ServicioId != null || d.PaqueteId != null)
                    .Sum(d => (decimal?)d.PrecioUnitario * d.Cantidad) ?? 0
            })
            .ToListAsync();

        var agendamientosQuery = await _context.Agendamientos.AsNoTracking()
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Where(a => a.FechaHora >= fechaInicio &&
                        a.Estado != null && a.Estado.ToLower() == "completada" &&
                        (a.ServicioId != null || a.PaqueteId != null))
            .Select(a => new {
                BarberoNombre = a.Barbero != null && a.Barbero.Usuario != null ? (a.Barbero.Usuario.Nombre + " " + a.Barbero.Usuario.Apellido) : "",
                Precio = (decimal?)a.Precio ?? 0
            })
            .ToListAsync();

        if (barbero != "Todos")
        {
            ventasQuery = ventasQuery.Where(v => MatchBarbero(v.BarberoNombre, barbero)).ToList();
            agendamientosQuery = agendamientosQuery.Where(a => MatchBarbero(a.BarberoNombre, barbero)).ToList();
        }

        decimal totalVentas = ventasQuery.Sum(v => v.TotalServicios);
        decimal totalAgendamientos = agendamientosQuery.Sum(a => a.Precio);

        decimal totalServicios;
        if (barbero != "Todos")
        {
            totalServicios = Math.Max(totalVentas, totalAgendamientos);
        }
        else
        {
            totalServicios = totalVentas > 0 ? totalVentas : totalAgendamientos;
        }

        decimal gananciasBarberos = totalServicios * 0.60m;
        decimal gananciasBarberia = totalServicios * 0.40m;

        return ServiceResult<object>.Ok(new
        {
            totalServicios,
            gananciasBarberos,
            gananciasBarberia,
            totalVentas,
            totalAgendamientos,
            periodo,
            barbero
        });
    }

    private bool MatchBarbero(string nombreVenta, string nombreFiltro)
    {
        if (string.IsNullOrEmpty(nombreVenta) || string.IsNullOrEmpty(nombreFiltro))
            return false;

        var v = nombreVenta.Trim().ToLower();
        var f = nombreFiltro.Trim().ToLower();

        return v == f || v.Contains(f) || f.Contains(v);
    }
}
