using BarberiaApi.Application.DTOs;
using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Application.Interfaces;

public interface ICategoriaService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> CreateAsync(Categoria categoria);
    Task<ServiceResult<object>> UpdateAsync(int id, Categoria categoria);
    Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input);
    Task<ServiceResult<object>> DeleteAsync(int id);
}
