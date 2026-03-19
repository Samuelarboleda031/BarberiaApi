using BarberiaApi.Application.DTOs;
using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Application.Interfaces;

public interface IModuloService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> CreateAsync(Modulos modulo);
    Task<ServiceResult<object>> UpdateAsync(int id, Modulos modulo);
    Task<ServiceResult<object>> DeleteAsync(int id);
}
