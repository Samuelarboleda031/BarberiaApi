using System.Globalization;
using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class DescuentoDiaService : IDescuentoDiaService
{
    private readonly BarberiaContext _context;

    public DescuentoDiaService(BarberiaContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<object>> GetAllAsync()
    {
        var items = await _context.DescuentosDia.AsNoTracking().ToListAsync();
        // Mapa { "yyyy-MM-dd": porcentaje } — formato que consumen front y móvil directamente.
        var map = items.ToDictionary(d => d.Fecha.ToString("yyyy-MM-dd"), d => d.Porcentaje);
        return ServiceResult<object>.Ok(map);
    }

    public async Task<ServiceResult<object>> UpsertAsync(DescuentoDiaInput input)
    {
        if (!DateOnly.TryParseExact(input.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            return ServiceResult<object>.Fail("Fecha inválida. Formato esperado: yyyy-MM-dd");

        if (input.Porcentaje < 0 || input.Porcentaje > 100)
            return ServiceResult<object>.Fail("El porcentaje debe estar entre 0 y 100");

        var existing = await _context.DescuentosDia.FirstOrDefaultAsync(d => d.Fecha == fecha);

        // Porcentaje 0 = sin descuento → eliminar el registro si existe.
        if (input.Porcentaje <= 0)
        {
            if (existing != null)
            {
                _context.DescuentosDia.Remove(existing);
                await _context.SaveChangesAsync();
            }
            return ServiceResult<object>.Ok(new { fecha = input.Fecha, porcentaje = 0m });
        }

        if (existing == null)
        {
            existing = new DescuentoDia { Fecha = fecha, Porcentaje = input.Porcentaje };
            _context.DescuentosDia.Add(existing);
        }
        else
        {
            existing.Porcentaje = input.Porcentaje;
        }

        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { fecha = input.Fecha, porcentaje = input.Porcentaje });
    }

    public async Task<ServiceResult<object>> DeleteAsync(string fecha)
    {
        if (!DateOnly.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var f))
            return ServiceResult<object>.Fail("Fecha inválida. Formato esperado: yyyy-MM-dd");

        var existing = await _context.DescuentosDia.FirstOrDefaultAsync(d => d.Fecha == f);
        if (existing == null)
            return ServiceResult<object>.Ok(new { eliminado = false });

        _context.DescuentosDia.Remove(existing);
        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { eliminado = true });
    }
}
