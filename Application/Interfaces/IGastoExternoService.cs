using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BarberiaApi.Application.Common;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface IGastoExternoService
{
    Task<ServiceResult<GastoExternoDto>> CreateAsync(GastoExternoInput input, int usuarioId);
    Task<ServiceResult<GastoExternoDto>> UpdateAsync(int id, GastoExternoInput input, int usuarioId);
    Task<ServiceResult<bool>> DeleteAsync(int id);  // Soft delete
    Task<ServiceResult<GastoExternoDto>> GetByIdAsync(int id);
    Task<ServiceResult<IEnumerable<GastoExternoDto>>> GetByDateAsync(DateOnly fecha);
    Task<ServiceResult<IEnumerable<GastoExternoDto>>> GetByDateRangeAsync(DateOnly from, DateOnly to);
    Task<ServiceResult<decimal>> GetTotalByDateAsync(DateOnly fecha);
    Task<ServiceResult<ResumenDiaDto>> GetResumenDiaAsync(DateOnly fecha);
}
