using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BarberiaApi.Application.Services;

public class CreditoBarberoService : ICreditoBarberoService
{
    private readonly BarberiaContext _context;
    private readonly INotificacionCreditoService _notificaciones;
    private readonly ILogger<CreditoBarberoService> _logger;

    public CreditoBarberoService(
        BarberiaContext context,
        INotificacionCreditoService notificaciones,
        ILogger<CreditoBarberoService> logger)
    {
        _context = context;
        _notificaciones = notificaciones;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────
    // CÁLCULO DE ESTADO CENTRALIZADO
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Recalcula el estado del crédito según las reglas de negocio.
    /// Retorna true si el estado cambió.
    /// </summary>
    private static bool RecalcularEstado(CreditoBarbero c)
    {
        var ahora = DateTime.UtcNow.AddHours(-5);
        var plazoVencido   = ahora > c.FechaVencimiento && c.SaldoPendiente > 0;
        var superaMitad    = c.SaldoPendiente > c.LimiteCredito / 2;  // > $100.000
        var vencidoYBloqueable = plazoVencido && superaMitad;          // vencido Y debe más de la mitad
        var limiteAlcanzado    = c.SaldoPendiente >= c.LimiteCredito;

        string nuevoEstado;
        if (c.SaldoPendiente == 0)
            nuevoEstado = "Pagado";
        else if (limiteAlcanzado && vencidoYBloqueable)
            nuevoEstado = "BloqueadoLimiteYVencimiento";
        else if (limiteAlcanzado)
            nuevoEstado = "BloqueadoLimite";
        else if (vencidoYBloqueable)
            nuevoEstado = "BloqueadoVencimiento";
        else
            nuevoEstado = "Activo";

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
                   c.Estado == "Activo" ||
                   c.Estado == "BloqueadoLimite" ||
                   c.Estado == "BloqueadoVencimiento" ||
                   c.Estado == "BloqueadoLimiteYVencimiento")
               ?? creditos.First();
    }

    // ─────────────────────────────────────────────────────────────
    // QUERIES
    // ─────────────────────────────────────────────────────────────

    private static int EstadoPrioridad(string estado)
    {
        if (estado.StartsWith("Bloqueado", StringComparison.OrdinalIgnoreCase)) return 0;
        if (string.Equals(estado, "Activo", StringComparison.OrdinalIgnoreCase)) return 1;
        if (string.Equals(estado, "Pagado", StringComparison.OrdinalIgnoreCase)) return 2;
        return 3;
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

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
        var items = porBarbero
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToList();

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
                FechaCreacion = DateTime.UtcNow.AddHours(-5),
                FechaInicio = DateTime.UtcNow.AddHours(-5),
                FechaVencimiento = DateTime.UtcNow.AddHours(-5).AddDays(7)
            });
        }

        return ServiceResult<object>.Ok(ToDto(credito));
    }

    public async Task<ServiceResult<object>> GetAbonosAsync(int barberoId, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

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
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

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
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

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
            var ahora = DateTime.UtcNow.AddHours(-5);
            credito = new CreditoBarbero
            {
                BarberoId = barberoId,
                LimiteCredito = 200000,
                SaldoPendiente = 0,
                PlazoDias = 7,
                FechaInicio = ahora,
                FechaVencimiento = ahora.AddDays(7),
                Estado = "Activo",
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

        var estadoAnterior = credito.Estado;
        var montoAplicable = input.Monto;
        credito.SaldoPendiente -= montoAplicable;

        RecalcularEstado(credito);

        if (credito.Estado == "Pagado" && credito.FechaCierre == null)
            credito.FechaCierre = DateTime.UtcNow.AddHours(-5);

        var abono = new AbonoCreditoBarbero
        {
            CreditoBarberoId = credito.Id,
            UsuarioId = input.UsuarioId,
            VentaId = input.VentaId,
            Monto = montoAplicable,
            MetodoPago = input.MetodoPago ?? "Efectivo",
            Fecha = DateTime.UtcNow.AddHours(-5),
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
                FechaCambio = DateTime.UtcNow.AddHours(-5),
                ResponsableId = input.UsuarioId,
                Observacion = $"Abono de {montoAplicable:C} registrado"
            });
        }

        _context.AbonosCreditoBarbero.Add(abono);
        await _context.SaveChangesAsync();

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

    // ─────────────────────────────────────────────────────────────
    // EXTENSIÓN DE PLAZO
    // ─────────────────────────────────────────────────────────────

    public async Task<ServiceResult<object>> ExtenderPlazoAsync(int barberoId, ExtenderPlazoInput input)
    {
        var usuario = await _context.Usuarios.FindAsync(input.UsuarioId);
        if (usuario == null) return ServiceResult<object>.Fail("El usuario no existe");

        var credito = await ObtenerCreditoVigenteAsync(barberoId);
        if (credito == null) return ServiceResult<object>.NotFound();

        if (credito.Estado == "Pagado")
            return ServiceResult<object>.Fail("El crédito ya está pagado; no se puede extender");

        if (credito.Estado != "BloqueadoVencimiento")
            return ServiceResult<object>.Fail(
                $"La extensión de plazo solo está disponible cuando el crédito está en estado 'BloqueadoVencimiento'. Estado actual: '{credito.Estado}'.");

        if (credito.ExtensionUsada)
            return ServiceResult<object>.Fail("Este ciclo ya tuvo una extensión de plazo. Solo se permite una por ciclo");

        var estadoAnterior = credito.Estado;
        credito.PlazoDias += 7;
        credito.FechaVencimiento = credito.FechaInicio.AddDays(credito.PlazoDias);
        credito.ExtensionUsada = true;

        RecalcularEstado(credito);

        _context.HistorialEstadoCredito.Add(new HistorialEstadoCredito
        {
            CreditoBarberoId = credito.Id,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = credito.Estado,
            FechaCambio = DateTime.UtcNow.AddHours(-5),
            ResponsableId = input.UsuarioId,
            Observacion = $"Plazo extendido de 7 a 14 días. Nueva fecha vencimiento: {credito.FechaVencimiento:dd/MM/yyyy}"
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
                           (c.Estado == "Activo" ||
                            c.Estado == "BloqueadoLimite" ||
                            c.Estado == "BloqueadoVencimiento" ||
                            c.Estado == "BloqueadoLimiteYVencimiento"));

        if (cicloVigente)
            return ServiceResult<object>.Fail("El barbero ya tiene un ciclo de crédito activo o bloqueado. Ciérrelo antes de abrir uno nuevo");

        var ahora = DateTime.UtcNow.AddHours(-5);
        var limite = input.LimiteCredito ?? 200000m;

        var nuevoCiclo = new CreditoBarbero
        {
            BarberoId = barberoId,
            LimiteCredito = limite,
            SaldoPendiente = 0,
            PlazoDias = input.PlazoDias,
            FechaInicio = ahora,
            FechaVencimiento = ahora.AddDays(input.PlazoDias),
            Estado = "Activo",
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
    // JOB PERIÓDICO — RECALCULAR VENCIMIENTOS
    // ─────────────────────────────────────────────────────────────

    public async Task RecalcularEstadosVencidosAsync(CancellationToken ct = default)
    {
        var ahora = DateTime.UtcNow.AddHours(-5);

        // Traer todos los créditos que no están pagados y tienen saldo
        var creditos = await _context.CreditosBarbero
            .Include(c => c.Barbero).ThenInclude(b => b.Usuario)
            .Where(c => c.Estado != "Pagado" && c.SaldoPendiente > 0)
            .ToListAsync(ct);

        var cambiados = new List<CreditoBarbero>();
        foreach (var c in creditos)
        {
            var cambio = RecalcularEstado(c);
            if (cambio) cambiados.Add(c);
        }

        if (cambiados.Count == 0) return;

        await _context.SaveChangesAsync(ct);

        // Notificar a los que quedaron bloqueados por vencimiento
        foreach (var c in cambiados.Where(c =>
                     c.Estado == "BloqueadoVencimiento" ||
                     c.Estado == "BloqueadoLimiteYVencimiento"))
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
