using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace BarberiaApi.Application.Services;

public class AgendamientoService : IAgendamientoService
{
    private readonly BarberiaContext _context;
    private readonly INotificacionCitasService _notificacionService;
    private readonly IMapper _mapper;

    public AgendamientoService(BarberiaContext context, INotificacionCitasService notificacionService, IMapper mapper)
    {
        _context = context;
        _notificacionService = notificacionService;
        _mapper = mapper;
    }

    private List<int> BuildServicioIds(AgendamientoInput input)
    {
        var ids = new List<int>();
        if (input.ServicioIds != null && input.ServicioIds.Count > 0)
        {
            ids.AddRange(input.ServicioIds.Where(id => id > 0));
        }
        if (input.ServicioId.HasValue && input.ServicioId.Value > 0)
        {
            ids.Add(input.ServicioId.Value);
        }
        return ids.Distinct().ToList();
    }

    private List<int> ExtractServicioIds(Agendamiento agendamiento)
    {
        var ids = new List<int>();
        if (agendamiento.ServicioId.HasValue && agendamiento.ServicioId.Value > 0)
        {
            ids.Add(agendamiento.ServicioId.Value);
        }
        if (agendamiento.AgendamientoServicios != null)
        {
            ids.AddRange(agendamiento.AgendamientoServicios.Select(s => s.ServicioId));
        }
        // Incluir servicios del paquete si existe
        if (agendamiento.Paquete != null && agendamiento.Paquete.DetallePaquetes != null)
        {
            ids.AddRange(agendamiento.Paquete.DetallePaquetes.Select(dp => dp.ServicioId));
        }
        return ids.Distinct().ToList();
    }
    
    private List<int> ExtractProductoIds(Agendamiento agendamiento)
    {
        if (agendamiento.AgendamientoProductos == null) return new List<int>();
        // Expand quantities: if Cantidad=3 for ProductoId=5, return [5, 5, 5]
        return agendamiento.AgendamientoProductos
            .SelectMany(ap => Enumerable.Repeat(ap.ProductoId, Math.Max(ap.Cantidad, 1)))
            .ToList();
    }

    /// <summary>
    /// Devuelve [{ProductoId, Cantidad}] desde input.Productos (nuevo) o input.ProductoIds (legacy).
    /// Si ambos vienen, usa Productos[].
    /// </summary>
    private static List<(int ProductoId, int Cantidad)> NormalizarProductosInput(AgendamientoInput input)
    {
        if (input.Productos != null && input.Productos.Count > 0)
        {
            return input.Productos
                .GroupBy(p => p.ProductoId)
                .Select(g => (g.Key, Math.Max(1, g.Sum(x => x.Cantidad))))
                .ToList();
        }
#pragma warning disable CS0618
        if (input.ProductoIds != null && input.ProductoIds.Count > 0)
        {
            return input.ProductoIds
                .GroupBy(pid => pid)
                .Select(g => (g.Key, g.Count()))
                .ToList();
        }
#pragma warning restore CS0618
        return new List<(int, int)>();
    }

    private List<int> ExtractMetaIds(string? notas, string prefix, int? singleId)
    {
        var ids = new List<int>();
        if (singleId.HasValue && singleId.Value > 0)
        {
            ids.Add(singleId.Value);
        }
        var text = notas ?? string.Empty;
        var start = text.IndexOf(prefix, StringComparison.Ordinal);
        if (start >= 0)
        {
            var end = text.IndexOf(']', start);
            if (end > start)
            {
                var payload = text.Substring(start + prefix.Length, end - (start + prefix.Length));
                var parsed = payload.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                    .Where(x => x > 0);
                ids.AddRange(parsed);
            }
        }
        return ids.Distinct().ToList();
    }

    private string BuildNotasWithMetadata(string? notasUsuario, List<int> servicioIds, List<int>? productoIds)
    {
        return CleanNotasFromMeta(notasUsuario);
    }


    private string CleanNotasFromMeta(string? notas)
    {
        if (string.IsNullOrWhiteSpace(notas)) return string.Empty;
        var lines = notas.Split('\n');
        var cleanLines = lines.Where(l => 
            !l.Trim().StartsWith("[SERVICIOS_IDS:", StringComparison.Ordinal) &&
            !l.Trim().StartsWith("[PRODUCTOS_IDS:", StringComparison.Ordinal));
        return string.Join("\n", cleanLines).Trim();
    }

    private async Task<Dictionary<int, Servicio>> LoadServiciosMapAsync(IEnumerable<Agendamiento> agendamientos)
    {
        var ids = agendamientos
            .SelectMany(a => ExtractServicioIds(a))
            .Distinct()
            .ToList();
        if (ids.Count == 0) return new Dictionary<int, Servicio>();
        return await _context.Servicios
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);
    }

    private async Task<Dictionary<int, Producto>> LoadProductosMapAsync(IEnumerable<Agendamiento> agendamientos)
    {
        var ids = agendamientos
            .SelectMany(a => ExtractProductoIds(a))
            .Distinct()
            .ToList();
        if (ids.Count == 0) return new Dictionary<int, Producto>();
        return await _context.Productos
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
    }

    private AgendamientoDTO MapToDto(Agendamiento agendamiento, Dictionary<int, Servicio> serviciosMap, Dictionary<int, Producto> productosMap)
    {
        var servicioIds = ExtractServicioIds(agendamiento);
        var productoIds = ExtractProductoIds(agendamiento);

        var serviciosNombres = servicioIds
            .Select(id => serviciosMap.TryGetValue(id, out var servicio) ? servicio.Nombre : null)
            .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
            .Select(nombre => nombre!)
            .ToList();

        var productosNombres = productoIds
            .Select(id => productosMap.TryGetValue(id, out var producto) ? producto.Nombre : null)
            .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
            .Select(nombre => nombre!)
            .ToList();

        var servicioNombre = serviciosNombres.Count > 0
            ? string.Join(", ", serviciosNombres)
            : (agendamiento.Servicio != null ? agendamiento.Servicio.Nombre : null);

        if (string.IsNullOrWhiteSpace(servicioNombre) && agendamiento.Paquete != null)
        {
            servicioNombre = agendamiento.Paquete.Nombre;
        }

        // Productos con cantidad e imagen (estructura nueva)
        var productosDetalle = (agendamiento.AgendamientoProductos ?? new List<AgendamientoProducto>())
            .Select(ap =>
            {
                productosMap.TryGetValue(ap.ProductoId, out var p);
                return new AgendamientoProductoDTO
                {
                    ProductoId = ap.ProductoId,
                    Nombre = p?.Nombre ?? string.Empty,
                    Cantidad = ap.Cantidad,
                    Imagen = p?.ImagenProduc,
                    PrecioVenta = p?.PrecioVenta
                };
            })
            .ToList();

        // Servicios con duración e imagen (estructura nueva)
        var serviciosDetalle = servicioIds
            .Select(sid =>
            {
                serviciosMap.TryGetValue(sid, out var s);
                return new AgendamientoServicioDTO
                {
                    ServicioId = sid,
                    Nombre = s?.Nombre ?? string.Empty,
                    Duracion = s?.DuracionMinutos,
                    Imagen = s?.Imagen,
                    Precio = s?.Precio
                };
            })
            .ToList();

        return new AgendamientoDTO
        {
            Id = agendamiento.Id,
            ClienteId = agendamiento.ClienteId,
            BarberoId = agendamiento.BarberoId,
            ServicioId = agendamiento.ServicioId,
            ServicioIds = servicioIds,
            ProductoIds = productoIds,
            Productos = productosDetalle,
            Servicios = serviciosDetalle,
            PaqueteId = agendamiento.PaqueteId,
            ClienteNombre = agendamiento.Cliente?.Usuario?.Nombre + " " + agendamiento.Cliente?.Usuario?.Apellido,
            BarberoNombre = agendamiento.Barbero?.Usuario?.Nombre + " " + agendamiento.Barbero?.Usuario?.Apellido,
            ServicioNombre = servicioNombre,
            ServiciosNombres = serviciosNombres,
            ProductosNombres = productosNombres,
            PaqueteNombre = agendamiento.Paquete?.Nombre,
            FechaHora = agendamiento.FechaHora,
            Estado = agendamiento.Estado,
            Duracion = agendamiento.Duracion,
            Precio = agendamiento.Precio,
            Notas = CleanNotasFromMeta(agendamiento.Notas)
        };
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q, bool? estaSemana)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 100;
        if (pageSize > 300) pageSize = 300;
        
        var baseQ = _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.AgendamientoProductos)
            .Include(a => a.AgendamientoServicios)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete).ThenInclude(p => p.DetallePaquetes).ThenInclude(dp => dp.Servicio)
            .AsNoTracking()
            .AsSplitQuery()
            .AsQueryable();

        if (estaSemana == true)
        {
            var now = DateTime.Now;
            int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = now.AddDays(-1 * diff).Date;
            var endOfWeek = startOfWeek.AddDays(7);
            baseQ = baseQ.Where(a => a.FechaHora >= startOfWeek && a.FechaHora < endOfWeek);
        }
        else if (estaSemana == false)
        {
            // Omitir filtro de tiempo para mostrar "todas"
        }
        else 
        {
            // Comportamiento por defecto (ej. últimos 30 días para no saturar si no se pide "todas")
            // Pero según el requerimiento, "todas" es sin el filtro de semana.
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(a =>
                (a.Estado != null && a.Estado.ToLower().Contains(term)) ||
                (a.Cliente != null && a.Cliente.Usuario != null && (
                    (a.Cliente.Usuario.Nombre != null && a.Cliente.Usuario.Nombre.ToLower().Contains(term)) ||
                    (a.Cliente.Usuario.Apellido != null && a.Cliente.Usuario.Apellido.ToLower().Contains(term))
                )) ||
                (a.Barbero != null && a.Barbero.Usuario != null && (
                    (a.Barbero.Usuario.Nombre != null && a.Barbero.Usuario.Nombre.ToLower().Contains(term)) ||
                    (a.Barbero.Usuario.Apellido != null && a.Barbero.Usuario.Apellido.ToLower().Contains(term))
                )) ||
                (a.Servicio != null && a.Servicio.Nombre != null && a.Servicio.Nombre.ToLower().Contains(term)) ||
                (a.Paquete != null && a.Paquete.Nombre != null && a.Paquete.Nombre.ToLower().Contains(term))
            );
        }
        var totalCount = await baseQ.CountAsync();
        var rows = await baseQ
            .OrderByDescending(a => a.FechaHora)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var serviciosMap = await LoadServiciosMapAsync(rows);
        var productosMap = await LoadProductosMapAsync(rows);
        var items = rows.Select(a => MapToDto(a, serviciosMap, productosMap)).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var agendamiento = await _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.AgendamientoProductos)
            .Include(a => a.AgendamientoServicios)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete).ThenInclude(p => p.DetallePaquetes).ThenInclude(dp => dp.Servicio)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamiento == null) return ServiceResult<object>.NotFound();
        var serviciosMap = await LoadServiciosMapAsync(new[] { agendamiento });
        var productosMap = await LoadProductosMapAsync(new[] { agendamiento });
        return ServiceResult<object>.Ok(MapToDto(agendamiento, serviciosMap, productosMap));
    }

    public async Task<ServiceResult<object>> GetByBarberoYFechaAsync(int barberoId, DateTime fecha)
    {
        var inicioDia = fecha.Date;
        var finDia = inicioDia.AddDays(1);

        var rows = await _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.AgendamientoProductos)
            .Include(a => a.AgendamientoServicios)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete).ThenInclude(p => p.DetallePaquetes).ThenInclude(dp => dp.Servicio)
            .Where(a => a.BarberoId == barberoId && a.FechaHora >= inicioDia && a.FechaHora < finDia)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();
        var serviciosMap = await LoadServiciosMapAsync(rows);
        var productosMap = await LoadProductosMapAsync(rows);
        return ServiceResult<object>.Ok(rows.Select(a => MapToDto(a, serviciosMap, productosMap)).ToList());
    }

    public async Task<ServiceResult<object>> GetByClienteAsync(int clienteId, int page, int pageSize, string? q, bool? estaSemana)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 100;
        if (pageSize > 300) pageSize = 300;

        var baseQ = _context.Agendamientos
            .Where(a => a.ClienteId == clienteId)
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.AgendamientoProductos)
            .Include(a => a.AgendamientoServicios)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete).ThenInclude(p => p.DetallePaquetes).ThenInclude(dp => dp.Servicio)
            .AsNoTracking()
            .AsSplitQuery()
            .AsQueryable();

        if (estaSemana == true)
        {
            var now = DateTime.Now;
            int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = now.AddDays(-1 * diff).Date;
            var endOfWeek = startOfWeek.AddDays(7);
            baseQ = baseQ.Where(a => a.FechaHora >= startOfWeek && a.FechaHora < endOfWeek);
        }


        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(a =>
                (a.Estado != null && a.Estado.ToLower().Contains(term)) ||
                (a.Barbero != null && a.Barbero.Usuario != null && (
                    (a.Barbero.Usuario.Nombre != null && a.Barbero.Usuario.Nombre.ToLower().Contains(term)) ||
                    (a.Barbero.Usuario.Apellido != null && a.Barbero.Usuario.Apellido.ToLower().Contains(term))
                )) ||
                (a.Servicio != null && a.Servicio.Nombre != null && a.Servicio.Nombre.ToLower().Contains(term)) ||
                (a.Paquete != null && a.Paquete.Nombre != null && a.Paquete.Nombre.ToLower().Contains(term))
            );
        }

        var totalCount = await baseQ.CountAsync();
        var rows = await baseQ
            .OrderByDescending(a => a.FechaHora)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var serviciosMap = await LoadServiciosMapAsync(rows);
        var productosMap = await LoadProductosMapAsync(rows);
        var items = rows.Select(a => MapToDto(a, serviciosMap, productosMap)).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> CreateAsync(AgendamientoInput input)
    {
        // NOTA: Validación estructural básica (ClienteId, BarberoId, FechaHora) 
        // ahora se maneja automáticamente por FluentValidation.

        var servicioIds = BuildServicioIds(input);
        if (servicioIds.Count > 0 && input.PaqueteId.HasValue)
            return ServiceResult<object>.Fail("No se puede agendar varios servicios y un paquete simultáneamente.");
        if (servicioIds.Count == 0 && !input.PaqueteId.HasValue)
            return ServiceResult<object>.Fail("Debe especificar al menos un servicio o un paquete.");

        var duracionMinutos = 30;
        decimal precioCalculado = 0m;
        if (servicioIds.Count > 0)
        {
            var servicios = await _context.Servicios
                .Where(s => servicioIds.Contains(s.Id))
                .ToListAsync();
            if (servicios.Count != servicioIds.Count) return ServiceResult<object>.Fail("Uno o más servicios no fueron encontrados.");
            duracionMinutos = servicios.Sum(s => s.DuracionMinutos ?? 30);
            precioCalculado = servicios.Sum(s => s.Precio);
        }
        else if (input.PaqueteId.HasValue)
        {
            var paquete = await _context.Paquetes.FindAsync(input.PaqueteId.Value);
            if (paquete == null) return ServiceResult<object>.Fail("Paquete no encontrado.");
            duracionMinutos = paquete.DuracionMinutos;
            precioCalculado = paquete.Precio;
        }

        var productosCantidad = NormalizarProductosInput(input);
        if (productosCantidad.Count > 0)
        {
            var ids = productosCantidad.Select(p => p.ProductoId).Distinct().ToList();
            var productos = await _context.Productos.Where(p => ids.Contains(p.Id)).ToListAsync();
            precioCalculado += productosCantidad.Sum(pc =>
            {
                var prod = productos.FirstOrDefault(p => p.Id == pc.ProductoId);
                return (prod?.PrecioVenta ?? 0) * pc.Cantidad;
            });
        }

        var barbero = await _context.Barberos.Include(b => b.Usuario).FirstOrDefaultAsync(b => b.Id == input.BarberoId);
        if (barbero == null) return ServiceResult<object>.Fail("Barbero no encontrado.");
        if (!barbero.Estado || !barbero.Usuario.Estado) return ServiceResult<object>.Fail("El barbero seleccionado no está activo.");

        var diaSemana = (int)input.FechaHora.DayOfWeek;
        if (diaSemana == 0) diaSemana = 7;

        var horario = await _context.HorariosBarberos
            .FirstOrDefaultAsync(h => h.BarberoId == input.BarberoId && h.DiaSemana == diaSemana && h.Estado == true);

        if (horario == null)
            return ServiceResult<object>.Fail("El barbero no trabaja en este día.");

        var horaCita = input.FechaHora.TimeOfDay;
        var horaFinCita = horaCita.Add(TimeSpan.FromMinutes(duracionMinutos));

        if (horaCita < horario.HoraInicio || horaFinCita > horario.HoraFin)
            return ServiceResult<object>.Fail($"La cita está fuera del horario de trabajo del barbero ({horario.HoraInicio:hh\\:mm} - {horario.HoraFin:hh\\:mm}).");

        var horaFin = input.FechaHora.AddMinutes(duracionMinutos);

        var existeTraslape = await _context.Agendamientos.AnyAsync(a =>
            a.BarberoId == input.BarberoId &&
            a.FechaHora < horaFin &&
            a.FechaHora.AddMinutes(duracionMinutos) > input.FechaHora &&
            a.Estado != "Cancelada");

        if (existeTraslape)
            return ServiceResult<object>.Fail("El barbero ya tiene una cita programada en ese horario.");

        var agendamiento = new Agendamiento
        {
            ClienteId = input.ClienteId,
            BarberoId = input.BarberoId,
            ServicioId = servicioIds.FirstOrDefault() > 0 ? servicioIds.FirstOrDefault() : null,
            PaqueteId = input.PaqueteId,
            FechaHora = input.FechaHora,
            Notas = BuildNotasWithMetadata(input.Notas, servicioIds, productosCantidad.Select(p => p.ProductoId).ToList()),
            Duracion = input.Duracion ?? $"{duracionMinutos} minutos",
            Precio = input.Precio ?? precioCalculado,
            Estado = "Pendiente"
        };

        if (servicioIds.Count > 0)
        {
            foreach (var sid in servicioIds)
            {
                agendamiento.AgendamientoServicios.Add(new AgendamientoServicio { ServicioId = sid });
            }
        }

        foreach (var pc in productosCantidad)
        {
            agendamiento.AgendamientoProductos.Add(new AgendamientoProducto { ProductoId = pc.ProductoId, Cantidad = pc.Cantidad });
        }

        _context.Agendamientos.Add(agendamiento);
        await _context.SaveChangesAsync();

        var created = await _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.AgendamientoProductos)
            .Include(a => a.AgendamientoServicios)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete).ThenInclude(p => p.DetallePaquetes).ThenInclude(dp => dp.Servicio)
            .Include(a => a.AgendamientoServicios)
            .FirstOrDefaultAsync(a => a.Id == agendamiento.Id);
        var serviciosMap = await LoadServiciosMapAsync(new[] { created! });
        var productosMap = await LoadProductosMapAsync(new[] { created! });
        return ServiceResult<object>.Ok(MapToDto(created!, serviciosMap, productosMap));
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, AgendamientoInput input)
    {
        var agendamientoExistente = await _context.Agendamientos
            .Include(a => a.Cliente)
            .Include(a => a.Barbero)
            .Include(a => a.AgendamientoProductos)
            .Include(a => a.AgendamientoServicios)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamientoExistente == null) return ServiceResult<object>.NotFound();

        var servicioIds = BuildServicioIds(input);
        if (servicioIds.Count > 0 && input.PaqueteId.HasValue)
            return ServiceResult<object>.Fail("No se puede agendar varios servicios y un paquete simultáneamente.");
        if (servicioIds.Count == 0 && !input.PaqueteId.HasValue)
            return ServiceResult<object>.Fail("Debe especificar al menos un servicio o un paquete.");

        var duracionMinutos = 30;
        decimal precioCalculado = agendamientoExistente.Precio ?? 0m;
        if (servicioIds.Count > 0)
        {
            var servicios = await _context.Servicios
                .Where(s => servicioIds.Contains(s.Id))
                .ToListAsync();
            if (servicios.Count != servicioIds.Count) return ServiceResult<object>.Fail("Uno o más servicios no fueron encontrados.");
            duracionMinutos = servicios.Sum(s => s.DuracionMinutos ?? 30);
            precioCalculado = servicios.Sum(s => s.Precio);
        }
        else if (input.PaqueteId.HasValue)
        {
            var paquete = await _context.Paquetes.FindAsync(input.PaqueteId.Value);
            if (paquete == null) return ServiceResult<object>.Fail("Paquete no encontrado.");
            duracionMinutos = paquete.DuracionMinutos;
            precioCalculado = paquete.Precio;
        }

        var productosCantidad = NormalizarProductosInput(input);
        if (productosCantidad.Count > 0)
        {
            var ids = productosCantidad.Select(p => p.ProductoId).Distinct().ToList();
            var productos = await _context.Productos.Where(p => ids.Contains(p.Id)).ToListAsync();
            precioCalculado += productosCantidad.Sum(pc =>
            {
                var prod = productos.FirstOrDefault(p => p.Id == pc.ProductoId);
                return (prod?.PrecioVenta ?? 0) * pc.Cantidad;
            });
        }

        var barberoNuevo = await _context.Barberos.Include(b => b.Usuario).FirstOrDefaultAsync(b => b.Id == input.BarberoId);
        if (barberoNuevo == null) return ServiceResult<object>.Fail("Barbero no encontrado.");
        if (!barberoNuevo.Estado || !barberoNuevo.Usuario.Estado) return ServiceResult<object>.Fail("El barbero seleccionado no está activo.");

        var diaSemana = (int)input.FechaHora.DayOfWeek;
        if (diaSemana == 0) diaSemana = 7;

        var horario = await _context.HorariosBarberos
            .FirstOrDefaultAsync(h => h.BarberoId == input.BarberoId && h.DiaSemana == diaSemana && h.Estado == true);

        if (horario == null)
            return ServiceResult<object>.Fail("El barbero no trabaja en este día.");

        var horaCita = input.FechaHora.TimeOfDay;
        var horaFinCita = horaCita.Add(TimeSpan.FromMinutes(duracionMinutos));

        if (horaCita < horario.HoraInicio || horaFinCita > horario.HoraFin)
            return ServiceResult<object>.Fail($"La cita está fuera del horario de trabajo del barbero ({horario.HoraInicio:hh\\:mm} - {horario.HoraFin:hh\\:mm}).");

        var horaFin = input.FechaHora.AddMinutes(duracionMinutos);
        var existeTraslape = await _context.Agendamientos.AnyAsync(a =>
            a.Id != id &&
            a.BarberoId == input.BarberoId &&
            a.FechaHora < horaFin &&
            a.FechaHora.AddMinutes(duracionMinutos) > input.FechaHora &&
            a.Estado != "Cancelada");

        if (existeTraslape)
            return ServiceResult<object>.Fail("El barbero ya tiene una cita programada en ese horario.");

        agendamientoExistente.ClienteId = input.ClienteId;
        agendamientoExistente.BarberoId = input.BarberoId;
        agendamientoExistente.ServicioId = servicioIds.FirstOrDefault() > 0 ? servicioIds.FirstOrDefault() : null;
        agendamientoExistente.PaqueteId = input.PaqueteId;
        agendamientoExistente.FechaHora = input.FechaHora;
        agendamientoExistente.Notas = BuildNotasWithMetadata(input.Notas, servicioIds, productosCantidad.Select(p => p.ProductoId).ToList());
        agendamientoExistente.Duracion = input.Duracion ?? $"{duracionMinutos} minutos";
        agendamientoExistente.Precio = input.Precio ?? precioCalculado;

        _context.AgendamientoServicios.RemoveRange(agendamientoExistente.AgendamientoServicios);
        if (servicioIds.Count > 0)
        {
            foreach (var sid in servicioIds)
            {
                agendamientoExistente.AgendamientoServicios.Add(new AgendamientoServicio { ServicioId = sid });
            }
        }

        _context.AgendamientoProductos.RemoveRange(agendamientoExistente.AgendamientoProductos);
        foreach (var pc in productosCantidad)
        {
            agendamientoExistente.AgendamientoProductos.Add(new AgendamientoProducto { ProductoId = pc.ProductoId, Cantidad = pc.Cantidad });
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Agendamientos.AnyAsync(e => e.Id == id))
                return ServiceResult<object>.NotFound();
            throw;
        }

        return ServiceResult<object>.Ok(new { success = true });
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoInput input)
    {
        var agendamiento = await _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.AgendamientoProductos)
            .Include(a => a.AgendamientoServicios)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete).ThenInclude(p => p.DetallePaquetes).ThenInclude(dp => dp.Servicio)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (agendamiento == null) return ServiceResult<object>.NotFound();

        var estadosValidos = new[] { "Pendiente", "Confirmada", "En Proceso", "Completada", "Cancelada" };
        if (!estadosValidos.Contains(input.estado))
            return ServiceResult<object>.Fail("Estado inválido.");

        var estadoAnterior = agendamiento.Estado ?? "Pendiente";
        agendamiento.Estado = input.estado;

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.SaveChangesAsync();

            if (string.Equals(input.estado, "Completada", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(estadoAnterior, "Completada", StringComparison.OrdinalIgnoreCase))
            {
                var usuarioId = agendamiento.Barbero?.UsuarioId ?? 0;
                if (usuarioId == 0) return ServiceResult<object>.Fail("El barbero asociado a la cita no tiene usuario válido.");

                var servicioIdsCita = ExtractServicioIds(agendamiento);
                var productoIdsCita = ExtractProductoIds(agendamiento);
                var serviciosCita = servicioIdsCita.Count > 0
                    ? await _context.Servicios.Where(s => servicioIdsCita.Contains(s.Id)).ToListAsync()
                    : new List<Servicio>();
                var productosCita = productoIdsCita.Count > 0
                    ? await _context.Productos.Where(p => productoIdsCita.Contains(p.Id)).ToListAsync()
                    : new List<Producto>();

                var serviciosPrecioMap = serviciosCita.ToDictionary(s => s.Id, s => s.Precio);
                var productosPrecioMap = productosCita.ToDictionary(p => p.Id, p => p.PrecioVenta);

                decimal precio = agendamiento.Precio.HasValue
                    ? agendamiento.Precio.Value
                    : (serviciosCita.Sum(s => s.Precio) + productosCita.Sum(p => p.PrecioVenta));

                var ventaExistente = await _context.Ventas
                    .Include(v => v.DetalleVenta)
                    .Where(v => v.ClienteId == agendamiento.ClienteId
                                && v.UsuarioId == usuarioId)
                    .Where(v => v.DetalleVenta.Any(d =>
                        (servicioIdsCita.Count > 0 && d.ServicioId.HasValue && servicioIdsCita.Contains(d.ServicioId.Value)) ||
                        (productoIdsCita.Count > 0 && d.ProductoId.HasValue && productoIdsCita.Contains(d.ProductoId.Value)) ||
                        (agendamiento.PaqueteId.HasValue && d.PaqueteId == agendamiento.PaqueteId)))
                    .OrderByDescending(v => v.Id)
                    .FirstOrDefaultAsync();
                if (ventaExistente != null)
                {
                    if (string.Equals(ventaExistente.Estado, "Anulada", StringComparison.OrdinalIgnoreCase))
                    {
                        ventaExistente.Estado = "Completada";
                        if (!ventaExistente.BarberoId.HasValue || ventaExistente.BarberoId.Value <= 0)
                        {
                            ventaExistente.BarberoId = agendamiento.BarberoId;
                        }
                        ventaExistente.Fecha = agendamiento.FechaHora;
                        if (!ventaExistente.DetalleVenta.Any())
                        {
                            if (servicioIdsCita.Count > 0)
                            {
                                var detallesReactivados = servicioIdsCita.Select(servicioId => new DetalleVenta
                                {
                                    VentaId = ventaExistente.Id,
                                    ServicioId = servicioId,
                                    PaqueteId = null,
                                    ProductoId = null,
                                    Cantidad = 1,
                                    PrecioUnitario = serviciosPrecioMap.TryGetValue(servicioId, out var pS) ? pS : 0m,
                                    Subtotal = serviciosPrecioMap.TryGetValue(servicioId, out var sSub) ? sSub : 0m
                                });
                                _context.DetalleVentas.AddRange(detallesReactivados);
                            }
                            if (productoIdsCita.Count > 0)
                            {
                                var detallesProds = productoIdsCita.Select(prodId => new DetalleVenta
                                {
                                    VentaId = ventaExistente.Id,
                                    ServicioId = null,
                                    PaqueteId = null,
                                    ProductoId = prodId,
                                    Cantidad = 1,
                                    PrecioUnitario = productosPrecioMap.TryGetValue(prodId, out var pV) ? pV : 0m,
                                    Subtotal = productosPrecioMap.TryGetValue(prodId, out var pSub) ? pSub : 0m
                                });
                                _context.DetalleVentas.AddRange(detallesProds);
                            }
                            if (servicioIdsCita.Count == 0 && productoIdsCita.Count == 0 && agendamiento.PaqueteId.HasValue)
                            {
                                var detalleReactivado = new DetalleVenta
                                {
                                    VentaId = ventaExistente.Id,
                                    ServicioId = agendamiento.ServicioId,
                                    PaqueteId = agendamiento.PaqueteId,
                                    ProductoId = null,
                                    Cantidad = 1,
                                    PrecioUnitario = precio,
                                    Subtotal = precio
                                };
                                _context.DetalleVentas.Add(detalleReactivado);
                            }
                        }
                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();
                        return ServiceResult<object>.Ok(new
                        {
                            message = "Estado actualizado. Venta existente reactivada",
                            estadoActual = input.estado,
                            agendamientoId = id,
                            ventaId = ventaExistente.Id,
                            venta = new
                            {
                                Id = ventaExistente.Id,
                                ClienteId = ventaExistente.ClienteId,
                                UsuarioId = ventaExistente.UsuarioId,
                                BarberoId = ventaExistente.BarberoId,
                                Fecha = ventaExistente.Fecha,
                                Subtotal = ventaExistente.Subtotal,
                                Total = ventaExistente.Total,
                                Estado = ventaExistente.Estado,
                                MetodoPago = ventaExistente.MetodoPago
                            }
                        });
                    }
                    else
                    {
                        await tx.CommitAsync();
                        return ServiceResult<object>.Ok(new
                        {
                            message = "Estado actualizado. Venta ya existente no duplicada",
                            estadoActual = input.estado,
                            agendamientoId = id,
                            ventaId = ventaExistente.Id,
                            venta = new
                            {
                                Id = ventaExistente.Id,
                                ClienteId = ventaExistente.ClienteId,
                                UsuarioId = ventaExistente.UsuarioId,
                                BarberoId = ventaExistente.BarberoId,
                                Fecha = ventaExistente.Fecha,
                                Subtotal = ventaExistente.Subtotal,
                                Total = ventaExistente.Total,
                                Estado = ventaExistente.Estado,
                                MetodoPago = ventaExistente.MetodoPago
                            }
                        });
                    }
                }

                var venta = new Venta
                {
                    UsuarioId = usuarioId,
                    ClienteId = agendamiento.ClienteId,
                    BarberoId = agendamiento.BarberoId,
                    Fecha = agendamiento.FechaHora,
                    Subtotal = precio,
                    IVA = 0m,
                    Descuento = 0m,
                    Total = precio,
                    MetodoPago = "Efectivo",
                    Estado = "Completada",
                    SaldoAFavorUsado = 0m
                };

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                if (servicioIdsCita.Count > 0)
                {
                    var detalles = servicioIdsCita.Select(servicioId => new DetalleVenta
                    {
                        VentaId = venta.Id,
                        ServicioId = servicioId,
                        PaqueteId = null,
                        ProductoId = null,
                        Cantidad = 1,
                        PrecioUnitario = serviciosPrecioMap.TryGetValue(servicioId, out var pS) ? pS : 0m,
                        Subtotal = serviciosPrecioMap.TryGetValue(servicioId, out var sSub) ? sSub : 0m
                    });
                    _context.DetalleVentas.AddRange(detalles);
                }
                if (productoIdsCita.Count > 0)
                {
                    var detallesProds = productoIdsCita.Select(prodId => new DetalleVenta
                    {
                        VentaId = venta.Id,
                        ServicioId = null,
                        PaqueteId = null,
                        ProductoId = prodId,
                        Cantidad = 1,
                        PrecioUnitario = productosPrecioMap.TryGetValue(prodId, out var pV) ? pV : 0m,
                        Subtotal = productosPrecioMap.TryGetValue(prodId, out var pSub) ? pSub : 0m
                    });
                    _context.DetalleVentas.AddRange(detallesProds);
                }
                if (servicioIdsCita.Count == 0 && productoIdsCita.Count == 0 && agendamiento.PaqueteId.HasValue)
                {
                    var detalle = new DetalleVenta
                    {
                        VentaId = venta.Id,
                        ServicioId = agendamiento.ServicioId,
                        PaqueteId = agendamiento.PaqueteId,
                        ProductoId = null,
                        Cantidad = 1,
                        PrecioUnitario = precio,
                        Subtotal = precio
                    };
                    _context.DetalleVentas.Add(detalle);
                }
                await _context.SaveChangesAsync();
            }
            if (string.Equals(input.estado, "Cancelada", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(estadoAnterior, "Cancelada", StringComparison.OrdinalIgnoreCase))
            {
                ResultadoNotificacionCita? notificacionCancelacion = null;

                var usuarioId = agendamiento.Barbero?.UsuarioId ?? 0;
                var servicioIdsCitaCancel = ExtractServicioIds(agendamiento);
                var productoIdsCitaCancel = ExtractProductoIds(agendamiento);
                var ventaRelacionada = await _context.Ventas
                    .Include(v => v.DetalleVenta)
                    .Where(v => v.ClienteId == agendamiento.ClienteId
                                && v.UsuarioId == usuarioId
                                && v.Estado != "Anulada")
                    .Where(v => v.DetalleVenta.Any(d =>
                        (servicioIdsCitaCancel.Count > 0 && d.ServicioId.HasValue && servicioIdsCitaCancel.Contains(d.ServicioId.Value)) ||
                        (productoIdsCitaCancel.Count > 0 && d.ProductoId.HasValue && productoIdsCitaCancel.Contains(d.ProductoId.Value)) ||
                        (agendamiento.PaqueteId.HasValue && d.PaqueteId == agendamiento.PaqueteId)))
                    .OrderByDescending(v => v.Id)
                    .FirstOrDefaultAsync();
                if (ventaRelacionada != null)
                {
                    ventaRelacionada.Estado = "Anulada";
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                    return ServiceResult<object>.Ok(new
                    {
                        message = "Estado actualizado. Venta asociada anulada",
                        estadoActual = input.estado,
                        agendamientoId = id,
                        ventaId = ventaRelacionada.Id,
                        venta = new
                        {
                            Id = ventaRelacionada.Id,
                            ClienteId = ventaRelacionada.ClienteId,
                            UsuarioId = ventaRelacionada.UsuarioId,
                            BarberoId = ventaRelacionada.BarberoId,
                            Fecha = ventaRelacionada.Fecha,
                            Subtotal = ventaRelacionada.Subtotal,
                            Total = ventaRelacionada.Total,
                            Estado = ventaRelacionada.Estado,
                            MetodoPago = ventaRelacionada.MetodoPago
                        },
                        notificacion = notificacionCancelacion == null ? null : new
                        {
                            enviado = notificacionCancelacion.Enviado,
                            canal = notificacionCancelacion.Canal,
                            mensaje = notificacionCancelacion.Mensaje
                        }
                    });
                }
                await tx.CommitAsync();
                return ServiceResult<object>.Ok(new
                {
                    message = "Estado actualizado correctamente",
                    estadoActual = input.estado,
                    agendamientoId = id,
                    notificacion = notificacionCancelacion == null ? null : new
                    {
                        enviado = notificacionCancelacion.Enviado,
                        canal = notificacionCancelacion.Canal,
                        mensaje = notificacionCancelacion.Mensaje
                    }
                });
            }

            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServiceResult<object>.Fail($"Error al actualizar estado y generar venta: {ex.Message}", 500);
        }

        return ServiceResult<object>.Ok(new
        {
            message = "Estado actualizado correctamente",
            estadoActual = input.estado,
            agendamientoId = id
        });
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var agendamiento = await _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.AgendamientoProductos)
            .Include(a => a.AgendamientoServicios)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamiento == null) return ServiceResult<object>.NotFound();

        ResultadoNotificacionCita? notificacionCancelacion = null;

        var infoRespuesta = new
        {
            message = "Agendamiento eliminado permanentemente",
            eliminado = true,
            fisico = true,
            clienteAsociado = agendamiento.Cliente?.Usuario?.Nombre + " " + agendamiento.Cliente?.Usuario?.Apellido,
            barberoAsociado = agendamiento.Barbero?.Usuario?.Nombre + " " + agendamiento.Barbero?.Usuario?.Apellido,
            servicioAsociado = agendamiento.Servicio?.Nombre,
            paqueteAsociado = agendamiento.Paquete?.Nombre,
            notificacion = notificacionCancelacion == null ? null : new
            {
                enviado = notificacionCancelacion.Enviado,
                canal = notificacionCancelacion.Canal,
                mensaje = notificacionCancelacion.Mensaje
            }
        };

        _context.Agendamientos.Remove(agendamiento);
        await _context.SaveChangesAsync();

        return ServiceResult<object>.Ok(infoRespuesta);
    }
}
