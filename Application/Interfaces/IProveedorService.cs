using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface IProveedorService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetNaturalesAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetJuridicosAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> CreateNaturalAsync(ProveedorNaturalInput input);
    Task<ServiceResult<object>> CreateJuridicoAsync(ProveedorJuridicoInput input);
    Task<ServiceResult<object>> CreateAsync(ProveedorCreateInput input);
    Task<ServiceResult<object>> UpdateAsync(int id, ProveedorUpdateInput input);
    Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input);
    Task<ServiceResult<object>> DeleteAsync(int id);
}
