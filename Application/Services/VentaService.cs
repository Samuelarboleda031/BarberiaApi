using BarberiaApi.Application.Common;
using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Constants;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using System.Data;

namespace BarberiaApi.Application.Services;

public class VentaService : IVentaService
{
    private readonly BarberiaContext _context;
    private readonly IMapper _mapper;
    private readonly INotificacionCreditoService _notificaciones;
    private readonly ILogger<VentaService> _logger;
    private readonly IDateTimeProvider _dt;

    public VentaService(
        BarberiaContext context,
        IMapper mapper,
        INotificacionCreditoService notificaciones,
        ILogger<VentaService> logger,
        IDateTimeProvider dt)
    {
        _context = context;
        _mapper = mapper;
        _notificaciones = notificaciones;
        _logger = logger;
        _dt = dt;
    }

    private static bool RecalcularEstadoCredito(CreditoBarbero c, DateTime ahora)
    {
        var plazoVencido       = ahora > c.FechaVencimiento && c.SaldoPendiente > 0;
        var superaMitad        = c.SaldoPendiente > c.LimiteCredito / 2;
        var vencidoYBloqueable = plazoVencido && superaMitad;
        var limiteAlcanzado    = c.SaldoPendiente >= c.LimiteCredito;

        string nuevoEstado;
        if (c.SaldoPendiente == 0)
            nuevoEstado = "Pagado";
        else if (limiteAlcanzado && vencidoYBloqueable)
            nuevoEstado = EstadosCredito.BloqueadoLimiteYVencimiento;
        else if (limiteAlcanzado)
            nuevoEstado = EstadosCredito.BloqueadoLimite;
        else if (vencidoYBloqueable)
            nuevoEstado = EstadosCredito.BloqueadoVencimiento;
        else
            nuevoEstado = "Activo";

        if (c.Estado == nuevoEstado) return false;
        c.Estado = nuevoEstado;
        return true;
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? searchTerm)
    {
        try
        {
            PaginationHelper.Sanitize(ref page, ref pageSize);

            var baseQ = _context.Ventas.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = $"%{searchTerm.Trim()}%";
                baseQ = baseQ.Where(v =>
                    (v.Estado != null && EF.Functions.Like(v.Estado, term)) ||
                    (v.MetodoPago != null && EF.Functions.Like(v.MetodoPago, term)) ||
                    (v.ClienteNombre != null && EF.Functions.Like(v.ClienteNombre, term)) ||
                    (v.TipoVenta != null && EF.Functions.Like(v.TipoVenta, term)) ||
                    (v.Cliente != null && v.Cliente.Usuario != null && (
                        (v.Cliente.Usuario.Nombre != null && EF.Functions.Like(v.Cliente.Usuario.Nombre, term)) ||
                        (v.Cliente.Usuario.Apellido != null && EF.Functions.Like(v.Cliente.Usuario.Apellido, term))
                    )) ||
                    (v.Usuario != null && (
                        (v.Usuario.Nombre != null && EF.Functions.Like(v.Usuario.Nombre, term)) ||
                        (v.Usuario.Apellido != null && EF.Functions.Like(v.Usuario.Apellido, term))
                    )) ||
                    (v.Barbero != null && v.Barbero.Usuario != null && (
                        (v.Barbero.Usuario.Nombre != null && EF.Functions.Like(v.Barbero.Usuario.Nombre, term)) ||
                        (v.Barbero.Usuario.Apellido != null && EF.Functions.Like(v.Barbero.Usuario.Apellido, term))
                    ))
                );
            }

            var totalCount = await baseQ.CountAsync();
            var items = await baseQ
                .OrderByDescending(v => v.Fecha)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<VentaDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
        }
        catch (Exception)
        {
            return ServiceResult<object>.Fail("Error al obtener ventas.", 500);
        }
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        try
        {
            var venta = await _context.Ventas
                .AsNoTracking().AsSplitQuery()
                .ProjectTo<VentaDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null) return ServiceResult<object>.NotFound();
            return ServiceResult<object>.Ok(venta);
        }
        catch (Exception)
        {
            return ServiceResult<object>.Fail("Error al obtener el detalle de la venta.", 500);
        }
    }

    public async Task<ServiceResult<object>> GetByClienteAsync(int clienteId, int page, int pageSize)
    {
        try
        {
            PaginationHelper.Sanitize(ref page, ref pageSize);

            var baseQ = _context.Ventas
                .AsNoTracking()
                .Where(v => v.ClienteId == clienteId);

            var totalCount = await baseQ.CountAsync();
            var items = await baseQ
                .OrderByDescending(v => v.Fecha)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<VentaDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
        }
        catch (Exception)
        {
            return ServiceResult<object>.Fail("Error al obtener ventas del cliente.", 500);
        }
    }

    public async Task<ServiceResult<object>> GetByAgendamientoAsync(int agendamientoId)
    {
        var ag = await _context.Agendamientos
            .Include(a => a.Barbero).Include(a => a.Cliente)
            .FirstOrDefaultAsync(a => a.Id == agendamientoId);

        if (ag == null) return ServiceResult<object>.NotFound();

        var usuarioId = ag.Barbero?.UsuarioId ?? 0;
        var ventaRelacionada = await _context.Ventas
            .Include(v => v.DetalleVenta)
            .Where(v => v.ClienteId == ag.ClienteId && v.UsuarioId == usuarioId)
            .Where(v => v.DetalleVenta.Any(d =>
                (ag.ServicioId.HasValue && d.ServicioId == ag.ServicioId) ||
                (ag.PaqueteId.HasValue && d.PaqueteId == ag.PaqueteId)))
            .OrderByDescending(v => v.Id)
            .FirstOrDefaultAsync();

        if (ventaRelacionada == null) return ServiceResult<object>.Ok(new { ventaId = 0 });
        return ServiceResult<object>.Ok(new
        {
            ventaId = ventaRelacionada.Id,
            venta = new
            {
                ventaRelacionada.Id,
                ventaRelacionada.ClienteId,
                ventaRelacionada.UsuarioId,
                ventaRelacionada.BarberoId,
                ventaRelacionada.Fecha,
                ventaRelacionada.Subtotal,
                ventaRelacionada.Total,
                ventaRelacionada.Estado,
                ventaRelacionada.MetodoPago
            }
        });
    }

    public async Task<ServiceResult<object>> CreateAsync(VentaInput input)
    {
        // NOTA: La validación estructural (Detalles != null, Cantidad > 0, etc) 
        // ahora se maneja automáticamente por FluentValidation en el Controller.

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            bool esVentaBarbero = string.Equals(input.TipoVenta, "Barbero", StringComparison.OrdinalIgnoreCase)
                || string.Equals(input.TipoVenta, "Venta Barbero", StringComparison.OrdinalIgnoreCase);

            if (esVentaBarbero)
            {
                if (!input.BarberoId.HasValue)
                    return ServiceResult<object>.Fail("Se requiere BarberoId para ventas de tipo Barbero");
                // Los barberos pueden comprar productos Y servicios
            }

            bool tieneServicioDirecto = input.Detalles.Any(d => d.ServicioId.HasValue);
            bool paqueteConServicio = false;
            var paqueteIds = input.Detalles.Where(d => d.PaqueteId.HasValue).Select(d => d.PaqueteId!.Value).Distinct().ToList();
            if (paqueteIds.Count > 0)
                paqueteConServicio = await _context.DetallePaquetes.AnyAsync(dp => paqueteIds.Contains(dp.PaqueteId) && dp.ServicioId != null);

            bool requiereBarbero = esVentaBarbero || tieneServicioDirecto || paqueteConServicio;
            if (requiereBarbero && !input.BarberoId.HasValue)
                return ServiceResult<object>.Fail("Se requiere BarberoId cuando la venta incluye servicios");

            int usuarioId = input.UsuarioId;
            if (requiereBarbero)
            {
                var barberoExistente = await _context.Barberos.FirstOrDefaultAsync(b => b.Id == input.BarberoId!.Value);
                if (barberoExistente == null)
                    return ServiceResult<object>.Fail("El barbero especificado no existe");
            }

            if (usuarioId == 0 && input.BarberoId.HasValue)
            {
                var barbero = await _context.Barberos.FirstOrDefaultAsync(b => b.Id == input.BarberoId.Value);
                if (barbero == null) return ServiceResult<object>.Fail("El barbero especificado no existe");
                usuarioId = barbero.UsuarioId;
                if (usuarioId == 0) return ServiceResult<object>.Fail("El barbero especificado no tiene usuario asociado valido");
            }
            else
            {
                var usuario = await _context.Usuarios.FindAsync(usuarioId);
                if (usuario == null) return ServiceResult<object>.Fail("El usuario especificado no existe");
            }

            string numeroRecibo = input.NumeroRecibo ?? string.Empty;
            if (string.IsNullOrWhiteSpace(numeroRecibo))
            {
                numeroRecibo = await GenerarNumeroReciboAsync();
            }
            else
            {
                var existe = await _context.Ventas.AnyAsync(v => v.NumeroRecibo == numeroRecibo);
                if (existe) return ServiceResult<object>.Fail($"NumeroRecibo '{numeroRecibo}' ya existe");
            }

            var venta = new Venta
            {
                NumeroRecibo = numeroRecibo,
                UsuarioId = usuarioId,
                ClienteId = input.ClienteId,
                BarberoId = input.BarberoId,
                BarberoPrestadorId = input.BarberoPrestadorId,
                Fecha = _dt.NowColombia,
                MetodoPago = input.MetodoPago ?? "Efectivo",
                TipoVenta = input.TipoVenta ?? (input.ClienteId.HasValue ? "Venta Cliente" : "Venta Invitado"),
                ClienteNombre = input.ClienteNombre,
                Descuento = input.Descuento ?? 0,
                IVA = 0,
                Estado = EstadosVenta.Completada
            };

            // NOTA: Cantidad <= 0 y PrecioUnitario < 0 ya validados por FluentValidation.
            
            var ventaProductIds = input.Detalles.Where(d => d.ProductoId.HasValue).Select(d => d.ProductoId!.Value).Distinct().ToList();
            var ventaProductos = ventaProductIds.Count > 0
                ? await _context.Productos.Where(p => ventaProductIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id)
                : new Dictionary<int, Domain.Entities.Producto>();

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
                    // Subtotal es columna computada en BD: ([Cantidad]*[PrecioUnitario]) — no asignar
                };
                // Calcular localmente solo para el total de la venta; BD usará su propio cómputo
                subtotal += detInput.Cantidad * detInput.PrecioUnitario;

                if (detInput.ProductoId.HasValue)
                {
                    if (!ventaProductos.TryGetValue(detInput.ProductoId.Value, out var producto))
                        return ServiceResult<object>.Fail($"Producto {detInput.ProductoId} no encontrado");
                    if (producto.Stock < detInput.Cantidad)
                        return ServiceResult<object>.Fail($"Stock insuficiente para el producto {producto.Nombre}");

                    if (esVentaBarbero)
                    {
                        detalle.PrecioUnitario = producto.PrecioCompra;
                        // BE-A1: NO asignar detalle.Subtotal — es columna computada en SQL
                        // (HasComputedColumnSql en BarberiaContext). El valor lo calcula la BD
                        // a partir de Cantidad * PrecioUnitario.
                    }

                    producto.Stock -= detInput.Cantidad;
                    producto.Stock = Math.Max(0, producto.Stock);
                }
                venta.DetalleVenta.Add(detalle);
            }

            if (esVentaBarbero)
                subtotal = venta.DetalleVenta.Sum(d => d.Cantidad * d.PrecioUnitario);

            venta.Subtotal = subtotal;
            venta.Total = (subtotal + venta.IVA.Value) - venta.Descuento.Value;

            var creditoBloquedoPorVenta = false;
            if (esVentaBarbero)
            {
                bool usaCredito = string.Equals(input.MetodoPago, "CreditoBarbero", StringComparison.OrdinalIgnoreCase);
                if (usaCredito)
                {
                    var credito = await _context.CreditosBarbero
                        .Where(c => c.BarberoId == input.BarberoId!.Value
                            && (c.Estado == EstadosCredito.Activo
                                || c.Estado == EstadosCredito.BloqueadoLimite
                                || c.Estado == EstadosCredito.BloqueadoVencimiento
                                || c.Estado == EstadosCredito.BloqueadoLimiteYVencimiento))
                        .OrderByDescending(c => c.FechaInicio)
                        .FirstOrDefaultAsync();

                    if (credito == null)
                    {
                        var plazo = input.PlazoDias is > 0 ? input.PlazoDias.Value : 7;
                        credito = new CreditoBarbero
                        {
                            BarberoId = input.BarberoId!.Value,
                            LimiteCredito = 200000,
                            SaldoPendiente = 0,
                            PlazoDias = plazo,
                            FechaInicio = _dt.NowColombia,
                            FechaVencimiento = _dt.NowColombia.AddDays(plazo),
                            Estado = "Activo",
                            CreadoPor = input.UsuarioId,
                            FechaCreacion = _dt.NowColombia
                        };
                        _context.CreditosBarbero.Add(credito);
                        await _context.SaveChangesAsync();
                    }

                    var esBloqueado = credito.Estado == EstadosCredito.BloqueadoLimite
                        || credito.Estado == EstadosCredito.BloqueadoVencimiento
                        || credito.Estado == EstadosCredito.BloqueadoLimiteYVencimiento;
                    if (esBloqueado)
                        return ServiceResult<object>.Fail($"El crédito del barbero está bloqueado (estado: {credito.Estado}). Debe realizar un abono antes de continuar.");

                    var nuevaDeuda = credito.SaldoPendiente + venta.Total;
                    if (nuevaDeuda > credito.LimiteCredito)
                        return ServiceResult<object>.Fail($"Cupo insuficiente. Deuda actual: {credito.SaldoPendiente:C}, esta venta: {venta.Total:C}, límite: {credito.LimiteCredito:C}");

                    credito.SaldoPendiente = nuevaDeuda;
                    var estadoAnteriorVenta = credito.Estado;
                    RecalcularEstadoCredito(credito, _dt.NowColombia);
                    if (credito.Estado != estadoAnteriorVenta && credito.Estado.StartsWith("Bloqueado"))
                        creditoBloquedoPorVenta = true;

                    venta.CreditoBarberoId = credito.Id;
                    venta.CreditoBarberoUsado = venta.Total;
                }
            }
            else if (input.ClienteId.HasValue)
            {
                var clienteId = input.ClienteId.Value;
                var totalDevoluciones = await _context.Devoluciones
                    .Where(d => d.ClienteId == clienteId && (d.Estado == "Activo" || d.Estado == EstadosAgendamiento.Completada || d.Estado == "Procesado"))
                    .SumAsync(d => d.SaldoAFavor ?? 0);
                var totalUsado = await _context.Ventas
                    .Where(v => v.ClienteId == clienteId && v.Estado != EstadosVenta.Anulada)
                    .SumAsync(v => v.SaldoAFavorUsado ?? 0);
                var disponible = Math.Max(0, totalDevoluciones - totalUsado);

                var solicitado = input.SaldoAFavorUsado ?? 0;
                decimal aplicable = 0;
                if (solicitado > 0)
                    aplicable = Math.Min(solicitado, Math.Min(disponible, venta.Total));
                else if (input.UsarSaldoAFavor == true || input.SaldoAFavorUsado == null)
                    aplicable = Math.Min(disponible, venta.Total);

                if (aplicable > 0)
                {
                    venta.SaldoAFavorUsado = aplicable;
                    venta.Total = Math.Max(0, venta.Total - aplicable);
                }
            }

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();

            // Capturar datos de notificación ANTES del commit, mientras el scope sigue vivo
            (int BarberoId, string Nombre, string Correo, decimal Saldo, decimal Limite, string? Telefono)? notifData = null;
            if (creditoBloquedoPorVenta && input.BarberoId.HasValue)
            {
                var credConBarbero = await _context.CreditosBarbero
                    .Include(c => c.Barbero).ThenInclude(b => b.Usuario)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.BarberoId == input.BarberoId.Value);

                if (credConBarbero?.Barbero?.Usuario is { } u)
                    notifData = (credConBarbero.BarberoId, $"{u.Nombre} {u.Apellido}".Trim(),
                        u.Correo ?? string.Empty, credConBarbero.SaldoPendiente,
                        credConBarbero.LimiteCredito, credConBarbero.Barbero?.Telefono);
            }

            await transaction.CommitAsync();

            // Enviar notificación después del commit, pero sin Task.Run — el scope sigue activo aquí
            if (notifData.HasValue)
            {
                try
                {
                    var n = notifData.Value;
                    await _notificaciones.NotificarCreditoBloqueadoAsync(
                        n.BarberoId, n.Nombre, n.Correo, n.Saldo, n.Limite, telefono: n.Telefono);
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning(notifEx, "Error enviando notificación de crédito bloqueado (venta).");
                }
            }

            // BE-A9: Retornar DTO, no la entidad EF completa
            var ventaDto = await _context.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente).ThenInclude(c => c!.Usuario)
                .Include(v => v.Usuario)
                .Include(v => v.Barbero).ThenInclude(b => b!.Usuario)
                .Include(v => v.BarberoPrestador).ThenInclude(b => b!.Usuario)
                .Include(v => v.DetalleVenta).ThenInclude(d => d.Producto)
                .Include(v => v.DetalleVenta).ThenInclude(d => d.Servicio)
                .Include(v => v.DetalleVenta).ThenInclude(d => d.Paquete)
                .Where(v => v.Id == venta.Id)
                .ProjectTo<VentaDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            return ServiceResult<object>.Ok(ventaDto!);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail("Error interno al procesar la venta.", 500);
        }
    }

    private async Task<string> GenerarNumeroReciboAsync()
    {
        var year = _dt.NowColombia.Year;
        var prefijo = $"REC-{year}-";

        var ultimo = await _context.Ventas
            .Where(v => v.NumeroRecibo != null && v.NumeroRecibo.StartsWith(prefijo))
            .OrderByDescending(v => v.Id)
            .Select(v => v.NumeroRecibo)
            .FirstOrDefaultAsync();

        int siguiente = 1;
        if (!string.IsNullOrEmpty(ultimo))
        {
            var partes = ultimo.Split('-');
            if (partes.Length == 3 && int.TryParse(partes[2], out int num))
                siguiente = num + 1;
        }
        return $"{prefijo}{siguiente:D6}";
    }

    public async Task<ServiceResult<object>> AnularAsync(int id)
    {
        var venta = await _context.Ventas
            .Include(v => v.DetalleVenta).ThenInclude(d => d.Producto)
            .Include(v => v.Cliente).Include(v => v.Usuario)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venta == null) return ServiceResult<object>.NotFound();
        if (venta.Estado == EstadosVenta.Anulada) return ServiceResult<object>.Fail("La venta ya esta anulada");

        if (venta.CreditoBarberoId.HasValue)
        {
            var tieneAbonoCompletado = await _context.AbonosCreditoBarbero
                .AnyAsync(a => a.CreditoBarberoId == venta.CreditoBarberoId.Value
                            && a.Estado == "Completado");
            if (tieneAbonoCompletado)
                return ServiceResult<object>.Fail($"La venta #{venta.Id} no puede anularse porque el crédito tiene abonos registrados. Los abonos no se pueden anular.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var estadoAnterior = venta.Estado;
            venta.Estado = EstadosVenta.Anulada;
            if ((venta.SaldoAFavorUsado ?? 0) > 0) venta.SaldoAFavorUsado = 0;

            if (venta.CreditoBarberoId.HasValue && (venta.CreditoBarberoUsado ?? 0) > 0)
            {
                var credito = await _context.CreditosBarbero.FindAsync(venta.CreditoBarberoId.Value);
                if (credito != null)
                {
                    credito.SaldoPendiente = Math.Max(0, credito.SaldoPendiente - venta.CreditoBarberoUsado!.Value);
                    RecalcularEstadoCredito(credito, _dt.NowColombia);
                    if (credito.Estado == "Pagado" && credito.FechaCierre == null)
                        credito.FechaCierre = _dt.NowColombia;
                }
                venta.CreditoBarberoUsado = 0;
            }

            foreach (var detalle in venta.DetalleVenta)
            {
                if (detalle.ProductoId.HasValue)
                {
                    var producto = await _context.Productos.FindAsync(detalle.ProductoId.Value);
                    if (producto != null)
                    {
                        producto.Stock += detalle.Cantidad;
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<object>.Ok(new CambioEstadoResponse<Venta>
            {
                entidad = venta,
                mensaje = $"Venta anulada exitosamente. Estado anterior: '{estadoAnterior}'",
                exitoso = true
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail("Error interno al procesar la venta.", 500);
        }
    }
}
