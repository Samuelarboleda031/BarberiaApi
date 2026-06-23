using BarberiaApi.Application.Common;
using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Constants;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BarberiaApi.Application.Services;

public class CreditoBarberoService : ICreditoBarberoService
{
    private readonly BarberiaContext _context;
    private readonly INotificacionCreditoService _notificaciones;
    private readonly ILogger<CreditoBarberoService> _logger;
    private readonly IDateTimeProvider _dt;

    public CreditoBarberoService(
        BarberiaContext context,
        INotificacionCreditoService notificaciones,
        ILogger<CreditoBarberoService> logger,
        IDateTimeProvider dt)
    {
        _context = context;
        _notificaciones = notificaciones;
        _logger = logger;
        _dt = dt;
    }

    // ─────────────────────────────────────────────────────────────
    // CÁLCULO DE ESTADO CENTRALIZADO
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Recalcula el estado del crédito según las reglas de negocio.
    /// Retorna true si el estado cambió.
    /// </summary>
    private static bool RecalcularEstado(CreditoBarbero c, DateTime ahora)
    {
        var plazoVencido   = ahora > c.FechaVencimiento && c.SaldoPendiente > 0;
        var superaMitad    = c.SaldoPendiente > c.LimiteCredito / 2;  // > $100.000
        var vencidoYBloqueable = plazoVencido && superaMitad;          // vencido Y debe más de la mitad
        var limiteAlcanzado    = c.SaldoPendiente >= c.LimiteCredito;

        string nuevoEstado;
        if (c.SaldoPendiente == 0)
            nuevoEstado = EstadosCredito.Pagado;
        else if (limiteAlcanzado && vencidoYBloqueable)
            nuevoEstado = EstadosCredito.BloqueadoLimiteYVencimiento;
        else if (limiteAlcanzado)
            nuevoEstado = EstadosCredito.BloqueadoLimite;
        else if (vencidoYBloqueable)
            nuevoEstado = EstadosCredito.BloqueadoVencimiento;
        else
            nuevoEstado = EstadosCredito.Activo;

        if (c.Estado == nuevoEstado) return false;
        c.Estado = nuevoEstado;
        return true;
    }

    private static CreditoBarberoDto ToDto(CreditoBarbero c) => new()
    {
        Id = c.Id,
        BarberoId = c.BarberoId,
        BarberoNombre = c.Barbero?.Usuario != null
            ? $"{c.Barbero.Usuario.Nombre} {c.Barbero.Usuario.Apellido}".Trim()
            : null,
        CupoMaximo = c.LimiteCredito,
        SaldoDeuda = c.SaldoPendiente,
        CupoDisponible = Math.Max(0, c.LimiteCredito - c.SaldoPendiente),
        Estado = c.Estado,
        PlazoDias = c.PlazoDias,
        FechaCreacion = c.FechaCreacion,
        FechaInicio = c.FechaInicio,
        FechaVencimiento = c.FechaVencimiento,
        FechaCierre = c.FechaCierre,
        ExtensionUsada = c.ExtensionUsada,
        FechaActualizacion = null
    };

    // ─────────────────────────────────────────────────────────────
    // OBTENER CRÉDITO ACTIVO/VIGENTE DE UN BARBERO
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna el crédito activo/bloqueado de un barbero.
    /// Si todos están Pagados, retorna el más reciente.
    /// </summary>
    private async Task<CreditoBarbero?> ObtenerCreditoVigenteAsync(int barberoId, bool tracking = true)
    {
        var q = _context.CreditosBarbero
            .Include(c => c.Barbero).ThenInclude(b => b.Usuario)
            .Where(c => c.BarberoId == barberoId);

        if (!tracking) q = q.AsNoTracking();

        var creditos = await q.OrderByDescending(c => c.FechaInicio).ToListAsync();
        if (!creditos.Any()) return null;

        return creditos.FirstOrDefault(c =>
                   c.Estado == EstadosCredito.Activo ||
                   c.Estado == EstadosCredito.BloqueadoLimite ||
                   c.Estado == EstadosCredito.BloqueadoVencimiento ||
                   c.Estado == EstadosCredito.BloqueadoLimiteYVencimiento)
               ?? creditos.First();
    }

    // ─────────────────────────────────────────────────────────────
    // QUERIES
    // ─────────────────────────────────────────────────────────────

    private static int EstadoPrioridad(string estado)
    {
        if (estado.StartsWith("Bloqueado", StringComparison.OrdinalIgnoreCase)) return 0;
        if (string.Equals(estado, "Activo", StringComparison.OrdinalIgnoreCase)) return 1;
        if (string.Equals(estado, EstadosCredito.Pagado, StringComparison.OrdinalIgnoreCase)) return 2;
        return 3;
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        PaginationHelper.Sanitize(ref page, ref pageSize);

        var baseQ = _context.CreditosBarbero
            .Include(c => c.Barbero).ThenInclude(b => b.Usuario)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(c =>
                (c.Barbero.Usuario.Nombre != null && c.Barbero.Usuario.Nombre.ToLower().Contains(term)) ||
                (c.Barbero.Usuario.Apellido != null && c.Barbero.Usuario.Apellido.ToLower().Contains(term)) ||
                (c.Estado != null && c.Estado.ToLower().Contains(term)));
        }

        // Un ciclo por barbero: el más reciente (independiente del estado)
        var all = await baseQ.OrderByDescending(c => c.FechaInicio).ToListAsync();

        var porBarbero = all
            .GroupBy(c => c.BarberoId)
            .Select(g => g.First())
            .OrderBy(c => EstadoPrioridad(c.Estado))
            .ThenByDescending(c => c.FechaInicio)
            .ToList();

        var totalCount = porBarbero.Count;
        var paginated = porBarbero.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var barberoIds = paginated.Select(c => c.BarberoId).ToList();

        // Batch 1: último abono por barbero en TODOS sus ciclos (2 queries, no N)
        var todosCiclosIds = await _context.CreditosBarbero
            .Where(c => barberoIds.Contains(c.BarberoId))
            .Select(c => new { c.Id, c.BarberoId })
            .ToListAsync();

        var cicloToBarberoId = todosCiclosIds.ToDictionary(x => x.Id, x => x.BarberoId);
        var allCicloIds = cicloToBarberoId.Keys.ToList();

        var ultimosAbonosRaw = await _context.AbonosCreditoBarbero
            .Where(a => allCicloIds.Contains(a.CreditoBarberoId))
            .GroupBy(a => a.CreditoBarberoId)
            .Select(g => new { CicloId = g.Key, Fecha = g.Max(a => a.Fecha) })
            .ToListAsync();

        var ultimoAbonoPorBarbero = new Dictionary<int, DateTime?>();
        foreach (var x in ultimosAbonosRaw)
        {
            if (!cicloToBarberoId.TryGetValue(x.CicloId, out var bid)) continue;
            if (!ultimoAbonoPorBarbero.ContainsKey(bid) || x.Fecha > ultimoAbonoPorBarbero[bid])
                ultimoAbonoPorBarbero[bid] = x.Fecha;
        }

        // Batch 2: conteo de ventas a crédito en el ciclo actual por barbero (1 query)
        var ventasCredito = await _context.Ventas
            .Where(v => barberoIds.Contains(v.BarberoId!.Value)
                        && v.MetodoPago == "CreditoBarbero"
                        && v.Estado != "Anulada")
            .Select(v => new { v.BarberoId, v.Fecha })
            .ToListAsync();

        var ventasCountPorBarbero = new Dictionary<int, int>();
        foreach (var c in paginated)
        {
            if (c.Estado == EstadosCredito.Pagado) { ventasCountPorBarbero[c.BarberoId] = 0; continue; }
            var inicio = c.FechaInicio.Date;
            var cierre = c.FechaCierre;
            ventasCountPorBarbero[c.BarberoId] = ventasCredito.Count(v =>
                v.BarberoId == c.BarberoId
                && v.Fecha >= inicio
                && (cierre == null || v.Fecha <= cierre));
        }

        var items = paginated.Select(c =>
        {
            var dto = ToDto(c);
            dto.UltimoAbono = ultimoAbonoPorBarbero.TryGetValue(c.BarberoId, out var ua) ? ua : null;
            dto.VentasCicloCount = ventasCountPorBarbero.TryGetValue(c.BarberoId, out var vc) ? vc : 0;
            return dto;
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByBarberoAsync(int barberoId)
    {
        var barbero = await _context.Barberos.FindAsync(barberoId);
        if (barbero == null) return ServiceResult<object>.NotFound();

        var credito = await ObtenerCreditoVigenteAsync(barberoId, tracking: false);

        if (credito == null)
        {
            return ServiceResult<object>.Ok(new CreditoBarberoDto
            {
                BarberoId = barberoId,
                CupoMaximo = 200000,
                SaldoDeuda = 0,
                CupoDisponible = 200000,
                Estado = "Sin crédito",
                PlazoDias = 7,
                FechaCreacion = _dt.NowColombia,
                FechaInicio = _dt.NowColombia,
                FechaVencimiento = _dt.NowColombia.AddDays(7)
            });
        }

        return ServiceResult<object>.Ok(ToDto(credito));
    }

    public async Task<ServiceResult<object>> GetAbonosAsync(int barberoId, int page, int pageSize)
    {
        PaginationHelper.Sanitize(ref page, ref pageSize);

        var credito = await ObtenerCreditoVigenteAsync(barberoId, tracking: false);
        if (credito == null) return ServiceResult<object>.NotFound();

        var baseQ = _context.AbonosCreditoBarbero
            .Include(a => a.Usuario)
            .Where(a => a.CreditoBarberoId == credito.Id)
            .AsNoTracking();

        var totalCount = await baseQ.CountAsync();
        var items = await baseQ
            .OrderByDescending(a => a.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AbonoCreditoBarberoDto
            {
                Id = a.Id,
                CreditoBarberoId = a.CreditoBarberoId,
                UsuarioId = a.UsuarioId,
                UsuarioNombre = a.Usuario != null ? a.Usuario.Nombre + " " + a.Usuario.Apellido : null,
                VentaId = a.VentaId,
                Monto = a.Monto,
                MetodoPago = a.MetodoPago,
                Fecha = a.Fecha,
                Notas = a.Notas,
                Estado = a.Estado
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetAllAbonosByBarberoAsync(int barberoId, int page, int pageSize)
    {
        PaginationHelper.Sanitize(ref page, ref pageSize);

        var cicloIds = await _context.CreditosBarbero
            .Where(c => c.BarberoId == barberoId)
            .Select(c => c.Id)
            .ToListAsync();

        if (!cicloIds.Any()) return ServiceResult<object>.NotFound();

        var baseQ = _context.AbonosCreditoBarbero
            .Include(a => a.Usuario)
            .Where(a => cicloIds.Contains(a.CreditoBarberoId))
            .AsNoTracking();

        var totalCount = await baseQ.CountAsync();
        var items = await baseQ
            .OrderByDescending(a => a.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AbonoCreditoBarberoDto
            {
                Id = a.Id,
                CreditoBarberoId = a.CreditoBarberoId,
                UsuarioId = a.UsuarioId,
                UsuarioNombre = a.Usuario != null ? a.Usuario.Nombre + " " + a.Usuario.Apellido : null,
                VentaId = a.VentaId,
                Monto = a.Monto,
                MetodoPago = a.MetodoPago,
                Fecha = a.Fecha,
                Notas = a.Notas,
                Estado = a.Estado
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetAbonosByCicloAsync(int cicloId, int page, int pageSize)
    {
        PaginationHelper.Sanitize(ref page, ref pageSize);

        var existe = await _context.CreditosBarbero.AsNoTracking().AnyAsync(c => c.Id == cicloId);
        if (!existe) return ServiceResult<object>.NotFound();

        var baseQ = _context.AbonosCreditoBarbero
            .Include(a => a.Usuario)
            .Where(a => a.CreditoBarberoId == cicloId)
            .AsNoTracking();

        var totalCount = await baseQ.CountAsync();
        var items = await baseQ
            .OrderByDescending(a => a.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AbonoCreditoBarberoDto
            {
                Id = a.Id,
                CreditoBarberoId = a.CreditoBarberoId,
                UsuarioId = a.UsuarioId,
                UsuarioNombre = a.Usuario != null ? a.Usuario.Nombre + " " + a.Usuario.Apellido : null,
                VentaId = a.VentaId,
                Monto = a.Monto,
                MetodoPago = a.MetodoPago,
                Fecha = a.Fecha,
                Notas = a.Notas,
                Estado = a.Estado
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetOrCreateAsync(int barberoId)
    {
        var barbero = await _context.Barberos.FindAsync(barberoId);
        if (barbero == null) return ServiceResult<object>.NotFound();

        var credito = await ObtenerCreditoVigenteAsync(barberoId);

        if (credito == null)
        {
            var ahora = _dt.NowColombia;
            credito = new CreditoBarbero
            {
                BarberoId = barberoId,
                LimiteCredito = 200000,
                SaldoPendiente = 0,
                PlazoDias = 7,
                FechaInicio = ahora,
                FechaVencimiento = ahora.AddDays(7),
                Estado = EstadosCredito.Activo,
                CreadoPor = 1,
                FechaCreacion = ahora
            };
            _context.CreditosBarbero.Add(credito);
            await _context.SaveChangesAsync();
        }

        return ServiceResult<object>.Ok(ToDto(credito));
    }

    // ─────────────────────────────────────────────────────────────
    // ABONOS
    // ─────────────────────────────────────────────────────────────

    public async Task<ServiceResult<object>> RegistrarAbonoAsync(int barberoId, AbonoInput input)
    {
        if (input.Monto <= 0)
            return ServiceResult<object>.Fail("El monto del abono debe ser mayor a 0");

        if (input.Monto < 5000)
            return ServiceResult<object>.Fail("El monto mínimo de abono es $5.000");

        if (input.Monto % 50 != 0)
            return ServiceResult<object>.Fail("El monto del abono debe ser múltiplo de $50 (denominaciones reales del peso colombiano: $50, $100, $200, $500, $1.000...)");


        var metodosValidos = new[] { "Efectivo", "Transferencia", "Tarjeta", "Nequi", "Daviplata", "Otro" };
        if (!string.IsNullOrWhiteSpace(input.MetodoPago) &&
            !metodosValidos.Contains(input.MetodoPago, StringComparer.OrdinalIgnoreCase))
            return ServiceResult<object>.Fail($"Método de pago no válido. Opciones: {string.Join(", ", metodosValidos)}");

        var usuario = await _context.Usuarios.FindAsync(input.UsuarioId);
        if (usuario == null) return ServiceResult<object>.Fail("El usuario no existe");

        var credito = await ObtenerCreditoVigenteAsync(barberoId);
        if (credito == null)
            return ServiceResult<object>.Fail("El barbero no tiene un crédito registrado");

        if (credito.SaldoPendiente <= 0)
            return ServiceResult<object>.Fail("El barbero no tiene deuda pendiente");

        // Si se vincula a una venta, verificar que exista y pertenezca al barbero
        if (input.VentaId.HasValue)
        {
            var ventaExiste = await _context.Ventas
                .AnyAsync(v => v.Id == input.VentaId.Value && v.BarberoId == barberoId);
            if (!ventaExiste)
                return ServiceResult<object>.Fail("La venta especificada no existe o no pertenece a este barbero");
        }

        if (input.Monto > credito.SaldoPendiente)
            return ServiceResult<object>.Fail(
                $"El monto del abono ({input.Monto:C}) supera el saldo pendiente ({credito.SaldoPendiente:C}). No se permiten abonos que excedan la deuda.");

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // Re-leer el crédito dentro de la transacción para evitar doble descuento concurrente
            credito = await ObtenerCreditoVigenteAsync(barberoId);
            if (credito == null)
            {
                await transaction.RollbackAsync();
                return ServiceResult<object>.Fail("El barbero no tiene un crédito registrado");
            }
            if (credito.SaldoPendiente <= 0)
            {
                await transaction.RollbackAsync();
                return ServiceResult<object>.Fail("El barbero no tiene deuda pendiente");
            }
            if (input.Monto > credito.SaldoPendiente)
            {
                await transaction.RollbackAsync();
                return ServiceResult<object>.Fail(
                    $"El monto del abono ({input.Monto:C}) supera el saldo pendiente ({credito.SaldoPendiente:C}). No se permiten abonos que excedan la deuda.");
            }

            var estadoAnterior = credito.Estado;
            var montoAplicable = input.Monto;
            credito.SaldoPendiente -= montoAplicable;

            RecalcularEstado(credito, _dt.NowColombia);

            if (credito.Estado == EstadosCredito.Pagado && credito.FechaCierre == null)
                credito.FechaCierre = _dt.NowColombia;

            var abono = new AbonoCreditoBarbero
            {
                CreditoBarberoId = credito.Id,
                UsuarioId = input.UsuarioId,
                VentaId = input.VentaId,
                Monto = montoAplicable,
                MetodoPago = input.MetodoPago ?? "Efectivo",
                Fecha = _dt.NowColombia,
                Notas = input.Notas,
                Estado = "Completado"
            };

            if (estadoAnterior != credito.Estado)
            {
                _context.HistorialEstadoCredito.Add(new HistorialEstadoCredito
                {
                    CreditoBarberoId = credito.Id,
                    EstadoAnterior = estadoAnterior,
                    EstadoNuevo = credito.Estado,
                    FechaCambio = _dt.NowColombia,
                    ResponsableId = input.UsuarioId,
                    Observacion = $"Abono de {montoAplicable:C} registrado"
                });
            }

            _context.AbonosCreditoBarbero.Add(abono);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<object>.Ok(new
            {
                mensaje = $"Abono de {montoAplicable:C} registrado exitosamente",
                abonoId = abono.Id,
                creditoBarberoId = credito.Id,
                saldoDeudaAnterior = credito.SaldoPendiente + montoAplicable,
                montoAbonado = montoAplicable,
                saldoDeudaActual = credito.SaldoPendiente,
                cupoDisponible = Math.Max(0, credito.LimiteCredito - credito.SaldoPendiente),
                estadoCredito = credito.Estado
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail("Error interno al registrar el abono.", 500);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // EXTENSIÓN DE PLAZO
    // ─────────────────────────────────────────────────────────────

    public async Task<ServiceResult<object>> ExtenderPlazoAsync(int barberoId, ExtenderPlazoInput input)
    {
        var usuario = await _context.Usuarios.FindAsync(input.UsuarioId);
        if (usuario == null) return ServiceResult<object>.Fail("El usuario no existe");

        var credito = await ObtenerCreditoVigenteAsync(barberoId);
        if (credito == null) return ServiceResult<object>.NotFound();

        if (credito.Estado == EstadosCredito.Pagado)
            return ServiceResult<object>.Fail("El crédito ya está pagado; no se puede extender");

        var ahora = _dt.NowColombia;
        if (ahora <= credito.FechaVencimiento)
            return ServiceResult<object>.Fail("El plazo del ciclo aún no ha vencido; no es necesario extender");

        if (credito.ExtensionUsada)
            return ServiceResult<object>.Fail("Este ciclo ya tuvo una extensión de plazo. Solo se permite una por ciclo");

        var estadoAnterior = credito.Estado;
        credito.PlazoDias += 7;
        credito.FechaVencimiento = ahora.AddDays(7);
        credito.ExtensionUsada = true;

        RecalcularEstado(credito, _dt.NowColombia);

        _context.HistorialEstadoCredito.Add(new HistorialEstadoCredito
        {
            CreditoBarberoId = credito.Id,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = credito.Estado,
            FechaCambio = _dt.NowColombia,
            ResponsableId = input.UsuarioId,
            Observacion = $"Plazo extendido +7 días desde hoy. Nueva fecha vencimiento: {credito.FechaVencimiento:dd/MM/yyyy}"
        });

        await _context.SaveChangesAsync();

        return ServiceResult<object>.Ok(new
        {
            mensaje = "Plazo extendido a 14 días exitosamente",
            creditoId = credito.Id,
            plazoDias = credito.PlazoDias,
            fechaVencimiento = credito.FechaVencimiento,
            estadoCredito = credito.Estado
        });
    }

    // ─────────────────────────────────────────────────────────────
    // NUEVO CICLO
    // ─────────────────────────────────────────────────────────────

    public async Task<ServiceResult<object>> NuevoCicloAsync(int barberoId, NuevoCicloInput input)
    {
        if (input.PlazoDias != 7 && input.PlazoDias != 14)
            return ServiceResult<object>.Fail("PlazoDias debe ser 7 o 14");

        var usuario = await _context.Usuarios.FindAsync(input.UsuarioId);
        if (usuario == null) return ServiceResult<object>.Fail("El usuario no existe");

        var barbero = await _context.Barberos.FindAsync(barberoId);
        if (barbero == null) return ServiceResult<object>.NotFound();

        // Verificar que no haya un ciclo activo/bloqueado en curso
        var cicloVigente = await _context.CreditosBarbero
            .AnyAsync(c => c.BarberoId == barberoId &&
                           (c.Estado == EstadosCredito.Activo ||
                            c.Estado == EstadosCredito.BloqueadoLimite ||
                            c.Estado == EstadosCredito.BloqueadoVencimiento ||
                            c.Estado == EstadosCredito.BloqueadoLimiteYVencimiento));

        if (cicloVigente)
            return ServiceResult<object>.Fail("El barbero ya tiene un ciclo de crédito activo o bloqueado. Ciérrelo antes de abrir uno nuevo");

        var ahora = _dt.NowColombia;
        var limite = input.LimiteCredito ?? 200000m;

        var nuevoCiclo = new CreditoBarbero
        {
            BarberoId = barberoId,
            LimiteCredito = limite,
            SaldoPendiente = 0,
            PlazoDias = input.PlazoDias,
            FechaInicio = ahora,
            FechaVencimiento = ahora.AddDays(input.PlazoDias),
            Estado = EstadosCredito.Activo,
            ExtensionUsada = false,
            CreadoPor = input.UsuarioId,
            FechaCreacion = ahora
        };

        _context.CreditosBarbero.Add(nuevoCiclo);
        await _context.SaveChangesAsync();

        _context.HistorialEstadoCredito.Add(new HistorialEstadoCredito
        {
            CreditoBarberoId = nuevoCiclo.Id,
            EstadoAnterior = "Ninguno",
            EstadoNuevo = "Activo",
            FechaCambio = ahora,
            ResponsableId = input.UsuarioId,
            Observacion = $"Nuevo ciclo creado. Límite: {limite:C}, Plazo: {input.PlazoDias} días"
        });

        await _context.SaveChangesAsync();

        return ServiceResult<object>.Ok(new
        {
            mensaje = "Nuevo ciclo de crédito creado exitosamente",
            creditoId = nuevoCiclo.Id,
            limiteCredito = nuevoCiclo.LimiteCredito,
            plazoDias = nuevoCiclo.PlazoDias,
            fechaVencimiento = nuevoCiclo.FechaVencimiento,
            estado = nuevoCiclo.Estado
        });
    }

    // ─────────────────────────────────────────────────────────────
    // SUBIR LÍMITE DE CRÉDITO
    // ─────────────────────────────────────────────────────────────

    public async Task<ServiceResult<object>> SubirLimiteAsync(int barberoId, SubirLimiteInput input)
    {
        if (input.Incremento <= 0 || input.Incremento % 10000 != 0)
            return ServiceResult<object>.Fail("El incremento debe ser un valor positivo múltiplo de $10.000");

        var usuario = await _context.Usuarios.FindAsync(input.UsuarioId);
        if (usuario == null) return ServiceResult<object>.Fail("El usuario no existe");

        var credito = await ObtenerCreditoVigenteAsync(barberoId);
        if (credito == null) return ServiceResult<object>.NotFound();

        if (credito.Estado == EstadosCredito.Pagado)
            return ServiceResult<object>.Fail("No se puede modificar un crédito que ya está pagado o cerrado");

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var estadoAnterior = credito.Estado;
            credito.LimiteCredito += input.Incremento;

            // Subir el límite SOLO levanta el bloqueo por límite. El bloqueo por
            // vencimiento (mora) no se toca: subir el cupo no perdona la deuda vencida.
            // No usamos RecalcularEstado aquí porque su regla `superaMitad` está acoplada
            // al límite y desbloquearía la mora por accidente al crecer el cupo.
            if (credito.Estado == EstadosCredito.BloqueadoLimite)
                credito.Estado = EstadosCredito.Activo;
            else if (credito.Estado == EstadosCredito.BloqueadoLimiteYVencimiento)
                credito.Estado = EstadosCredito.BloqueadoVencimiento;

            if (estadoAnterior != credito.Estado)
            {
                _context.HistorialEstadoCredito.Add(new HistorialEstadoCredito
                {
                    CreditoBarberoId = credito.Id,
                    EstadoAnterior = estadoAnterior,
                    EstadoNuevo = credito.Estado,
                    FechaCambio = _dt.NowColombia,
                    ResponsableId = input.UsuarioId,
                    Observacion = $"Límite aumentado +{input.Incremento:C}. Nuevo límite: {credito.LimiteCredito:C}"
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ServiceResult<object>.Ok(ToDto(credito));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Fail("Error interno al subir el límite.", 500);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // JOB PERIÓDICO — RECALCULAR VENCIMIENTOS
    // ─────────────────────────────────────────────────────────────

    public async Task RecalcularEstadosVencidosAsync(CancellationToken ct = default)
    {
        var ahora = _dt.NowColombia;

        // Traer todos los créditos que no están pagados y tienen saldo
        var creditos = await _context.CreditosBarbero
            .Include(c => c.Barbero).ThenInclude(b => b.Usuario)
            .Where(c => c.Estado != EstadosCredito.Pagado && c.SaldoPendiente > 0)
            .ToListAsync(ct);

        var cambiados = new List<CreditoBarbero>();
        foreach (var c in creditos)
        {
            var cambio = RecalcularEstado(c, ahora);
            if (cambio) cambiados.Add(c);
        }

        if (cambiados.Count == 0) return;

        await _context.SaveChangesAsync(ct);

        // Notificar a los que quedaron bloqueados por vencimiento
        foreach (var c in cambiados.Where(c =>
                     c.Estado == EstadosCredito.BloqueadoVencimiento ||
                     c.Estado == EstadosCredito.BloqueadoLimiteYVencimiento))
        {
            if (ct.IsCancellationRequested) break;
            var correo = c.Barbero?.Usuario?.Correo ?? string.Empty;
            var nombre = c.Barbero?.Usuario != null
                ? $"{c.Barbero.Usuario.Nombre} {c.Barbero.Usuario.Apellido}".Trim()
                : "Barbero";
            try
            {
                await _notificaciones.NotificarCreditoBloqueadoAsync(
                    c.BarberoId, nombre, correo, c.SaldoPendiente, c.LimiteCredito,
                    telefono: c.Barbero?.Telefono, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error notificando bloqueo por vencimiento a barberoId={Id}", c.BarberoId);
            }
        }

        _logger.LogInformation("RecalcularEstados: {N} créditos actualizados.", cambiados.Count);
    }
}
