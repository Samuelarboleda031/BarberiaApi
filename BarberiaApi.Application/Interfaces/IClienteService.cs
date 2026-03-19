using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface IClienteService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> GetSaldoDisponibleAsync(int id);
    Task<ServiceResult<object>> CreateAsync(ClienteInput input);
    Task<ServiceResult<object>> UpdateAsync(int id, ClienteInput input);
    Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input);
    Task<ServiceResult<object>> DeleteAsync(int id);
}
