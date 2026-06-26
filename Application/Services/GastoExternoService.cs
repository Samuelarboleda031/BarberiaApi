using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BarberiaApi.Application.Common;
using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace BarberiaApi.Application.Services;

public class GastoExternoService : IGastoExternoService
{
    private readonly BarberiaContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GastoExternoService> _logger;
    private readonly IDateTimeProvider _dt;

    public GastoExternoService(
        BarberiaContext context,
        IMapper mapper,
        ILogger<GastoExternoService> logger,
        IDateTimeProvider dt)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _dt = dt;
    }

    public async Task<ServiceResult<GastoExternoDto>> CreateAsync(GastoExternoInput input, int usuarioId)
    {
        try
        {
            if (!DateOnly.TryParseExact(input.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                return ServiceResult<GastoExternoDto>.Fail("Fecha inválida. Formato esperado: yyyy-MM-dd");

            var gasto = new GastoExterno
            {
                Descripcion = input.Descripcion,
                Monto = input.Monto,
                Categoria = input.Categoria,
                Fecha = fecha,
                UsuarioId = usuarioId,
                Notas = input.Notas,
                CreatedAt = _dt.NowColombia,
                Estado = true
            };

            _context.GastosExternos.Add(gasto);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Gasto externo creado: {gasto.Id} de ${gasto.Monto} en {gasto.Categoria}");
            
            var dto = await _context.GastosExternos.AsNoTracking()
                .Include(g => g.Usuario)
                .Where(g => g.Id == gasto.Id)
                .ProjectTo<GastoExternoDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            return ServiceResult<GastoExternoDto>.Ok(dto!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear gasto externo");
            return ServiceResult<GastoExternoDto>.Fail("Error al crear el gasto externo", 500);
        }
    }

    public async Task<ServiceResult<GastoExternoDto>> UpdateAsync(int id, GastoExternoInput input, int usuarioId)
    {
        try
        {
            if (!DateOnly.TryParseExact(input.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                return ServiceResult<GastoExternoDto>.Fail("Fecha inválida. Formato esperado: yyyy-MM-dd");

            var gasto = await _context.GastosExternos.FirstOrDefaultAsync(g => g.Id == id && g.Estado);
            if (gasto == null || gasto.DeletedAt.HasValue)
                return ServiceResult<GastoExternoDto>.NotFound();

            gasto.Descripcion = input.Descripcion;
            gasto.Monto = input.Monto;
            gasto.Categoria = input.Categoria;
            gasto.Fecha = fecha;
            gasto.Notas = input.Notas;
            gasto.UpdatedAt = _dt.NowColombia;

            await _context.SaveChangesAsync();

            var dto = await _context.GastosExternos.AsNoTracking()
                .Include(g => g.Usuario)
                .Where(g => g.Id == id)
                .ProjectTo<GastoExternoDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            return ServiceResult<GastoExternoDto>.Ok(dto!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al actualizar gasto externo {id}");
            return ServiceResult<GastoExternoDto>.Fail("Error al actualizar el gasto externo", 500);
        }
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id)
    {
        try
        {
            var gasto = await _context.GastosExternos.FirstOrDefaultAsync(g => g.Id == id && g.Estado);
            if (gasto == null || gasto.DeletedAt.HasValue)
                return ServiceResult<bool>.NotFound();

            gasto.DeletedAt = _dt.NowColombia;
            gasto.Estado = false;

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al eliminar gasto externo {id}");
            return ServiceResult<bool>.Fail("Error al eliminar el gasto externo", 500);
        }
    }

    public async Task<ServiceResult<GastoExternoDto>> GetByIdAsync(int id)
    {
        try
        {
            var dto = await _context.GastosExternos.AsNoTracking()
                .Include(g => g.Usuario)
                .Where(g => g.Id == id && g.Estado && !g.DeletedAt.HasValue)
                .ProjectTo<GastoExternoDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (dto == null)
                return ServiceResult<GastoExternoDto>.NotFound();

            return ServiceResult<GastoExternoDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al obtener gasto externo {id}");
            return ServiceResult<GastoExternoDto>.Fail("Error al obtener el gasto externo", 500);
        }
    }

    public async Task<ServiceResult<IEnumerable<GastoExternoDto>>> GetByDateAsync(DateOnly fecha)
    {
        try
        {
            var gastos = await _context.GastosExternos.AsNoTracking()
                .Include(g => g.Usuario)
                .Where(g => g.Fecha == fecha && g.Estado && !g.DeletedAt.HasValue)
                .OrderByDescending(g => g.CreatedAt)
                .ProjectTo<GastoExternoDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return ServiceResult<IEnumerable<GastoExternoDto>>.Ok(gastos.Cast<GastoExternoDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al obtener gastos para la fecha {fecha}");
            return ServiceResult<IEnumerable<GastoExternoDto>>.Fail("Error al obtener gastos", 500);
        }
    }

    public async Task<ServiceResult<IEnumerable<GastoExternoDto>>> GetByDateRangeAsync(DateOnly from, DateOnly to)
    {
        try
        {
            var gastos = await _context.GastosExternos.AsNoTracking()
                .Include(g => g.Usuario)
                .Where(g => g.Fecha >= from && g.Fecha <= to && g.Estado && !g.DeletedAt.HasValue)
                .OrderByDescending(g => g.Fecha)
                .ThenByDescending(g => g.CreatedAt)
                .ProjectTo<GastoExternoDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return ServiceResult<IEnumerable<GastoExternoDto>>.Ok(gastos.Cast<GastoExternoDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al obtener gastos para el rango {from} a {to}");
            return ServiceResult<IEnumerable<GastoExternoDto>>.Fail("Error al obtener gastos", 500);
        }
    }

    public async Task<ServiceResult<decimal>> GetTotalByDateAsync(DateOnly fecha)
    {
        try
        {
            var total = await _context.GastosExternos.AsNoTracking()
                .Where(g => g.Fecha == fecha && g.Estado && !g.DeletedAt.HasValue)
                .SumAsync(g => (decimal?)g.Monto) ?? 0m;

            return ServiceResult<decimal>.Ok(total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al obtener total de gastos para {fecha}");
            return ServiceResult<decimal>.Fail("Error al obtener total de gastos", 500);
        }
    }

    public async Task<ServiceResult<ResumenDiaDto>> GetResumenDiaAsync(DateOnly fecha)
    {
        try
        {
            var startOfDay = fecha.ToDateTime(TimeOnly.MinValue);
            var endOfDay = fecha.ToDateTime(TimeOnly.MaxValue);

            // Obtener gastos del día
            var gastosRes = await GetByDateAsync(fecha);
            if (!gastosRes.Success)
                return ServiceResult<ResumenDiaDto>.Fail(gastosRes.Error ?? "Error al obtener gastos", gastosRes.StatusCode);

            var gastosList = (gastosRes.Data ?? Array.Empty<GastoExternoDto>()).ToList();
            var totalGastos = gastosList.Sum(g => g.Monto);

            // Evitar doble conteo:
            // 1. Obtener los IDs de Ventas del día que están ligadas a algún agendamiento
            var linkedVentaIds = await _context.Agendamientos.AsNoTracking()
                .Where(a => a.FechaHora >= startOfDay && a.FechaHora <= endOfDay && a.VentaAsociadaId != null)
                .Select(a => a.VentaAsociadaId!.Value)
                .ToListAsync();

            // 2. Ingresos por ventas: Ventas activas del día que no correspondan a agendamientos agendados
            var ingresosVentas = await _context.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= startOfDay && v.Fecha <= endOfDay &&
                            v.Estado != "Anulada" && v.Estado != "Cancelada" &&
                            !linkedVentaIds.Contains(v.Id))
                .SumAsync(v => (decimal?)v.Total) ?? 0m;

            // 3. Ingresos por agendamientos: Agendamientos completados del día
            var ingresosAgendamientos = await _context.Agendamientos.AsNoTracking()
                .Where(a => a.FechaHora >= startOfDay && a.FechaHora <= endOfDay &&
                            a.Estado != null && a.Estado.ToLower() == "completada")
                .SumAsync(a => (decimal?)a.Precio) ?? 0m;

            var totalIngresos = ingresosVentas + ingresosAgendamientos;

            var resumen = new ResumenDiaDto
            {
                Fecha = fecha.ToString("yyyy-MM-dd"),
                IngresosVentas = ingresosVentas,
                IngresosAgendamientos = ingresosAgendamientos,
                IngresosTotal = totalIngresos,
                GastosExternos = totalGastos,
                GananciaNeta = totalIngresos - totalGastos,
                CantidadGastos = gastosList.Count,
                Gastos = gastosList
            };

            return ServiceResult<ResumenDiaDto>.Ok(resumen);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al generar resumen diario para {fecha}");
            return ServiceResult<ResumenDiaDto>.Fail("Error al generar el resumen diario", 500);
        }
    }
}
