using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface ICompraService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? searchTerm);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> CreateAsync(CompraInput input);
    Task<ServiceResult<object>> AnularAsync(int id);
}
