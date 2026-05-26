using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface ICreditoBarberoService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetByBarberoAsync(int barberoId);
    Task<ServiceResult<object>> GetAbonosAsync(int creditoBarberoId, int page, int pageSize);
    Task<ServiceResult<object>> RegistrarAbonoAsync(int barberoId, AbonoInput input);
    Task<ServiceResult<object>> GetOrCreateAsync(int barberoId);
    Task<ServiceResult<object>> AnularAbonoAsync(int abonoId, AnularAbonoInput input);
}
