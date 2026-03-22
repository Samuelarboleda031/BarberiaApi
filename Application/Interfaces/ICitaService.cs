using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface ICitaService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> CreateAsync(CitaInputFrontend input);
    Task<ServiceResult<object>> UpdateAsync(int id, CitaInputFrontend input);
    Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoInput input);
    Task<ServiceResult<object>> DeleteAsync(int id);
}
