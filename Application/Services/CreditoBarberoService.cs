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
                (c.Estado != null && c.Estado.ToLower().Contains(term))
            );
        }

        var totalCount = await baseQ.CountAsync();
        var items = await baseQ
            .OrderByDescending(c => c.SaldoDeuda)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CreditoBarberoDto
            {
                Id = c.Id,
                BarberoId = c.BarberoId,
                BarberoNombre = c.Barbero.Usuario.Nombre + " " + c.Barbero.Usuario.Apellido,
                CupoMaximo = c.CupoMaximo,
                SaldoDeuda = c.SaldoDeuda,
                CupoDisponible = c.CupoMaximo - c.SaldoDeuda,
                Estado = c.Estado,
                FechaCreacion = c.FechaCreacion,
                FechaActualizacion = c.FechaActualizacion
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByBarberoAsync(int barberoId)
    {
        var barbero = await _context.Barberos.FindAsync(barberoId);
        if (barbero == null) return ServiceResult<object>.NotFound();

        var credito = await _context.CreditosBarbero
            .Include(c => c.Barbero).ThenInclude(b => b.Usuario)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.BarberoId == barberoId);

        if (credito == null)
        {
            return ServiceResult<object>.Ok(new CreditoBarberoDto
            {
                BarberoId = barberoId,
                CupoMaximo = 200000,
                SaldoDeuda = 0,
                CupoDisponible = 200000,
                Estado = "Sin crédito"
            });
        }

        return ServiceResult<object>.Ok(new CreditoBarberoDto
        {
            Id = credito.Id,
            BarberoId = credito.BarberoId,
            BarberoNombre = credito.Barbero?.Usuario != null
                ? credito.Barbero.Usuario.Nombre + " " + credito.Barbero.Usuario.Apellido
                : null,
            CupoMaximo = credito.CupoMaximo,
            SaldoDeuda = credito.SaldoDeuda,
            CupoDisponible = credito.CupoMaximo - credito.SaldoDeuda,
            Estado = credito.Estado,
            FechaCreacion = credito.FechaCreacion,
            FechaActualizacion = credito.FechaActualizacion
        });
    }

    public async Task<ServiceResult<object>> GetAbonosAsync(int barberoId, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var credito = await _context.CreditosBarbero
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.BarberoId == barberoId);

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

        var credito = await _context.CreditosBarbero
            .Include(c => c.Barbero).ThenInclude(b => b.Usuario)
            .FirstOrDefaultAsync(c => c.BarberoId == barberoId);

        if (credito == null)
            return ServiceResult<object>.Fail("El barbero no tiene un crédito registrado");

        if (credito.SaldoDeuda <= 0)
            return ServiceResult<object>.Fail("El barbero no tiene deuda pendiente");

        var montoAplicable = Math.Min(input.Monto, credito.SaldoDeuda);
        credito.SaldoDeuda -= montoAplicable;
        credito.FechaActualizacion = DateTime.Now;

        if (credito.Estado == "Bloqueado" && credito.SaldoDeuda < credito.CupoMaximo)
            credito.Estado = "Activo";

        var abono = new AbonoCreditoBarbero
        {
            CreditoBarberoId = credito.Id,
            UsuarioId = input.UsuarioId,
            Monto = montoAplicable,
            MetodoPago = input.MetodoPago ?? "Efectivo",
            Fecha = DateTime.Now,
            Notas = input.Notas
        };

        _context.AbonosCreditoBarbero.Add(abono);
        await _context.SaveChangesAsync();

        return ServiceResult<object>.Ok(new
        {
            mensaje = $"Abono de {montoAplicable:C} registrado exitosamente",
            abonoId = abono.Id,
            creditoBarberoId = credito.Id,
            saldoDeudaAnterior = credito.SaldoDeuda + montoAplicable,
            montoAbonado = montoAplicable,
            saldoDeudaActual = credito.SaldoDeuda,
            cupoDisponible = credito.CupoMaximo - credito.SaldoDeuda,
            estadoCredito = credito.Estado
        });
    }

    public async Task<ServiceResult<object>> AnularAbonoAsync(int abonoId, AnularAbonoInput input)
    {
        var abono = await _context.AbonosCreditoBarbero
            .Include(a => a.CreditoBarbero)
            .FirstOrDefaultAsync(a => a.Id == abonoId);

        if (abono == null)
            return ServiceResult<object>.NotFound();

        if (abono.Estado == "Anulado")
            return ServiceResult<object>.Fail("El abono ya está anulado");

        var usuario = await _context.Usuarios.FindAsync(input.UsuarioId);
        if (usuario == null)
            return ServiceResult<object>.Fail("El usuario no existe");

        // Revertir el monto al saldo de deuda del crédito
        abono.Estado = "Anulado";
        abono.CreditoBarbero.SaldoDeuda += abono.Monto;
        abono.CreditoBarbero.FechaActualizacion = DateTime.Now;

        var quedaBloqueado = false;
        // Si el crédito superó el cupo, marcarlo como Bloqueado
        if (abono.CreditoBarbero.SaldoDeuda >= abono.CreditoBarbero.CupoMaximo)
        {
            abono.CreditoBarbero.Estado = "Bloqueado";
            quedaBloqueado = true;
        }

        await _context.SaveChangesAsync();

        if (quedaBloqueado)
        {
            var credConBarbero = await _context.CreditosBarbero
                .Include(c => c.Barbero).ThenInclude(b => b.Usuario)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == abono.CreditoBarberoId);

            if (credConBarbero?.Barbero?.Usuario is { } u)
            {
                var nombre = $"{u.Nombre} {u.Apellido}".Trim();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificaciones.NotificarCreditoBloqueadoAsync(
                            credConBarbero.BarberoId, nombre, u.Correo,
                            credConBarbero.SaldoDeuda, credConBarbero.CupoMaximo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error enviando notificación de crédito bloqueado (anulación abono).");
                    }
                });
            }
        }

        return ServiceResult<object>.Ok(new
        {
            mensaje = $"Abono #{abonoId} anulado. Saldo restaurado: {abono.Monto:C}",
            abonoId,
            montorevertido = abono.Monto,
            saldoDeudaActual = abono.CreditoBarbero.SaldoDeuda,
            estadoCredito = abono.CreditoBarbero.Estado
        });
    }

    public async Task<ServiceResult<object>> GetOrCreateAsync(int barberoId)
    {
        var barbero = await _context.Barberos.FindAsync(barberoId);
        if (barbero == null) return ServiceResult<object>.NotFound();

        var credito = await _context.CreditosBarbero
            .FirstOrDefaultAsync(c => c.BarberoId == barberoId);

        if (credito == null)
        {
            credito = new CreditoBarbero
            {
                BarberoId = barberoId,
                CupoMaximo = 200000,
                SaldoDeuda = 0,
                Estado = "Activo",
                FechaCreacion = DateTime.Now
            };
            _context.CreditosBarbero.Add(credito);
            await _context.SaveChangesAsync();
        }

        return ServiceResult<object>.Ok(new
        {
            credito.Id,
            credito.BarberoId,
            credito.CupoMaximo,
            credito.SaldoDeuda,
            cupoDisponible = credito.CupoMaximo - credito.SaldoDeuda,
            credito.Estado
        });
    }
}
