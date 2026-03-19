using BarberiaApi.Application.DTOs;
using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Application.Interfaces;

public interface IUsuarioService
{
    Task<ServiceResult<object>> AnalisisAsync(int page, int pageSize);
    Task<ServiceResult<object>> AnalisisPorIdAsync(int id);
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> CreateAsync(UsuarioInput input);
    Task<ServiceResult<object>> UpdateAsync(int id, UsuarioInput input);
    Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input);
    Task<ServiceResult<object>> DeleteAsync(int id);
}
