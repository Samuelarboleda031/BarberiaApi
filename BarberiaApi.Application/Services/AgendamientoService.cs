using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class AgendamientoService : IAgendamientoService
{
    private readonly BarberiaContext _context;
    private readonly INotificacionCitasService _notificacionService;
    private const string ServiciosMetaPrefix = "[SERVICIOS_IDS:";

    public AgendamientoService(BarberiaContext context, INotificacionCitasService notificacionService)
    {
        _context = context;
        _notificacionService = notificacionService;
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
        var notas = agendamiento.Notas ?? string.Empty;
        var start = notas.IndexOf(ServiciosMetaPrefix, StringComparison.Ordinal);
        if (start >= 0)
        {
            var end = notas.IndexOf(']', start);
            if (end > start)
            {
                var payload = notas.Substring(start + ServiciosMetaPrefix.Length, end - (start + ServiciosMetaPrefix.Length));
                var parsed = payload.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                    .Where(x => x > 0);
                ids.AddRange(parsed);
            }
        }
        return ids.Distinct().ToList();
    }

    private string BuildNotasWithServicios(string? notasUsuario, List<int> servicioIds)
    {
        var cleanNotas = CleanNotasFromMeta(notasUsuario);
        if (servicioIds == null || servicioIds.Count <= 1)
        {
            return cleanNotas;
        }
        var meta = $"{ServiciosMetaPrefix}{string.Join(",", servicioIds)}]";
        if (string.IsNullOrWhiteSpace(cleanNotas))
        {
            return meta;
        }
        return $"{meta}\n{cleanNotas}";
    }

    private string CleanNotasFromMeta(string? notas)
    {
        if (string.IsNullOrWhiteSpace(notas)) return string.Empty;
        var text = notas.Trim();
        if (text.StartsWith(ServiciosMetaPrefix, StringComparison.Ordinal))
        {
            var idx = text.IndexOf(']');
            if (idx >= 0)
            {
                text = text.Substring(idx + 1).TrimStart('\r', '\n', ' ');
            }
        }
        return text;
    }

    private async Task<Dictionary<int, Servicio>> LoadServiciosMapAsync(IEnumerable<Agendamiento> agendamientos)
    {
        var ids = agendamientos
            .SelectMany(a => ExtractServicioIds(a))
            .Concat(agendamientos.Where(a => a.ServicioId.HasValue).Select(a => a.ServicioId!.Value))
            .Distinct()
            .ToList();
        if (ids.Count == 0) return new Dictionary<int, Servicio>();
        return await _context.Servicios
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);
    }

    private AgendamientoDTO MapToDto(Agendamiento agendamiento, Dictionary<int, Servicio> serviciosMap)
    {
        var servicioIds = ExtractServicioIds(agendamiento);
        var serviciosNombres = servicioIds
            .Select(id => serviciosMap.TryGetValue(id, out var servicio) ? servicio.Nombre : null)
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
        return new AgendamientoDTO
        {
            Id = agendamiento.Id,
            ClienteId = agendamiento.ClienteId,
            BarberoId = agendamiento.BarberoId,
            ServicioId = agendamiento.ServicioId,
            ServicioIds = servicioIds,
            PaqueteId = agendamiento.PaqueteId,
            ClienteNombre = agendamiento.Cliente?.Usuario?.Nombre + " " + agendamiento.Cliente?.Usuario?.Apellido,
            BarberoNombre = agendamiento.Barbero?.Usuario?.Nombre + " " + agendamiento.Barbero?.Usuario?.Apellido,
            ServicioNombre = servicioNombre,
            ServiciosNombres = serviciosNombres,
            PaqueteNombre = agendamiento.Paquete?.Nombre,
            FechaHora = agendamiento.FechaHora,
            Estado = agendamiento.Estado,
            Duracion = agendamiento.Duracion,
            Precio = agendamiento.Precio,
            Notas = CleanNotasFromMeta(agendamiento.Notas)
        };
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 100;
        if (pageSize > 300) pageSize = 300;
        var limite = DateTime.Now.AddDays(-7);
        var baseQ = _context.Agendamientos
            .Where(a => a.FechaHora >= limite)
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete)
            .AsNoTracking()
            .AsSplitQuery()
            .AsQueryable();
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
        var items = rows.Select(a => MapToDto(a, serviciosMap)).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var agendamiento = await _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamiento == null) return ServiceResult<object>.NotFound();
        var serviciosMap = await LoadServiciosMapAsync(new[] { agendamiento });
        return ServiceResult<object>.Ok(MapToDto(agendamiento, serviciosMap));
    }

    public async Task<ServiceResult<object>> GetByBarberoYFechaAsync(int barberoId, DateTime fecha)
    {
        var inicioDia = fecha.Date;
        var finDia = inicioDia.AddDays(1);

        var rows = await _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete)
            .Where(a => a.BarberoId == barberoId && a.FechaHora >= inicioDia && a.FechaHora < finDia)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();
        var serviciosMap = await LoadServiciosMapAsync(rows);
        return ServiceResult<object>.Ok(rows.Select(a => MapToDto(a, serviciosMap)).ToList());
    }

    public async Task<ServiceResult<object>> GetByClienteAsync(int clienteId, int page, int pageSize, string? q)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 100;
        if (pageSize > 300) pageSize = 300;

        var baseQ = _context.Agendamientos
            .Where(a => a.ClienteId == clienteId)
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete)
            .AsNoTracking()
            .AsSplitQuery()
            .AsQueryable();

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
        var items = rows.Select(a => MapToDto(a, serviciosMap)).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> CreateAsync(AgendamientoInput input)
    {
        if (input == null) return ServiceResult<object>.Fail("Input requerido");

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
            duracionMinutos = servicios.Sum(s => s.DuracionMinutes ?? 30);
            precioCalculado = servicios.Sum(s => s.Precio);
        }
        else if (input.PaqueteId.HasValue)
        {
            var paquete = await _context.Paquetes.FindAsync(input.PaqueteId.Value);
            if (paquete == null) return ServiceResult<object>.Fail("Paquete no encontrado.");
            duracionMinutos = paquete.DuracionMinutos;
            precioCalculado = paquete.Precio;
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
            Notas = BuildNotasWithServicios(input.Notas, servicioIds),
            Duracion = input.Duracion ?? $"{duracionMinutos} minutos",
            Precio = input.Precio ?? precioCalculado,
            Estado = "Pendiente"
        };

        _context.Agendamientos.Add(agendamiento);
        await _context.SaveChangesAsync();

        var created = await _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete)
            .FirstOrDefaultAsync(a => a.Id == agendamiento.Id);
        var serviciosMap = await LoadServiciosMapAsync(new[] { created! });
        return ServiceResult<object>.Ok(MapToDto(created!, serviciosMap));
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, AgendamientoInput input)
    {
        var agendamientoExistente = await _context.Agendamientos
            .Include(a => a.Cliente)
            .Include(a => a.Barbero)
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
            duracionMinutos = servicios.Sum(s => s.DuracionMinutes ?? 30);
            precioCalculado = servicios.Sum(s => s.Precio);
        }
        else if (input.PaqueteId.HasValue)
        {
            var paquete = await _context.Paquetes.FindAsync(input.PaqueteId.Value);
            if (paquete == null) return ServiceResult<object>.Fail("Paquete no encontrado.");
            duracionMinutos = paquete.DuracionMinutos;
            precioCalculado = paquete.Precio;
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
        agendamientoExistente.Notas = BuildNotasWithServicios(input.Notas, servicioIds);
        agendamientoExistente.Duracion = input.Duracion ?? $"{duracionMinutos} minutos";
        agendamientoExistente.Precio = input.Precio ?? precioCalculado;

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
            .Include(a => a.Servicio)
            .Include(a => a.Paquete)
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
                var serviciosCita = servicioIdsCita.Count > 0
                    ? await _context.Servicios.Where(s => servicioIdsCita.Contains(s.Id)).ToListAsync()
                    : new List<Servicio>();
                var serviciosPrecioMap = serviciosCita.ToDictionary(s => s.Id, s => s.Precio);
                decimal precio = agendamiento.Precio.HasValue
                    ? agendamiento.Precio.Value
                    : (serviciosCita.Count > 0
                        ? serviciosCita.Sum(s => s.Precio)
                        : (agendamiento.Paquete != null ? agendamiento.Paquete.Precio : 0m));

                var ventaExistente = await _context.Ventas
                    .Include(v => v.DetalleVenta)
                    .Where(v => v.ClienteId == agendamiento.ClienteId
                                && v.UsuarioId == usuarioId)
                    .Where(v => v.DetalleVenta.Any(d =>
                        (servicioIdsCita.Count > 0 && d.ServicioId.HasValue && servicioIdsCita.Contains(d.ServicioId.Value)) ||
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
                                    Cantidad = 1,
                                    PrecioUnitario = serviciosPrecioMap.TryGetValue(servicioId, out var precioServicio) ? precioServicio : 0m
                                });
                                _context.DetalleVentas.AddRange(detallesReactivados);
                            }
                            else
                            {
                                var detalleReactivado = new DetalleVenta
                                {
                                    VentaId = ventaExistente.Id,
                                    ServicioId = agendamiento.ServicioId,
                                    PaqueteId = agendamiento.PaqueteId,
                                    Cantidad = 1,
                                    PrecioUnitario = precio
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
                        Cantidad = 1,
                        PrecioUnitario = serviciosPrecioMap.TryGetValue(servicioId, out var precioServicio) ? precioServicio : 0m
                    });
                    _context.DetalleVentas.AddRange(detalles);
                }
                else
                {
                    var detalle = new DetalleVenta
                    {
                        VentaId = venta.Id,
                        ServicioId = agendamiento.ServicioId,
                        PaqueteId = agendamiento.PaqueteId,
                        Cantidad = 1,
                        PrecioUnitario = precio
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
                var ventaRelacionada = await _context.Ventas
                    .Include(v => v.DetalleVenta)
                    .Where(v => v.ClienteId == agendamiento.ClienteId
                                && v.UsuarioId == usuarioId
                                && v.Estado != "Anulada")
                    .Where(v => v.DetalleVenta.Any(d =>
                        (servicioIdsCitaCancel.Count > 0 && d.ServicioId.HasValue && servicioIdsCitaCancel.Contains(d.ServicioId.Value)) ||
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
