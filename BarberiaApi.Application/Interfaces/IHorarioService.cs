using BarberiaApi.Application.DTOs;
using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Application.Interfaces;

public interface IHorarioService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> GetByBarberoAsync(int barberoId, int page, int pageSize, string? q);
    Task<ServiceResult<object>> CreateAsync(HorarioBarberoCreateInput input);
    Task<ServiceResult<object>> UpdateAsync(int id, HorarioBarberoUpdateInput input);
    Task<ServiceResult<object>> DeleteAsync(int id);
    Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoHorarioInput input);
    Task<ServiceResult<object>> GetDisponiblesAsync(string fecha);
    Task<ServiceResult<object>> CancelarDiaPorBarberoAsync(int barberoId, CambioEstadoHorarioInput input);
}
