using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface ICreditoBarberoService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q, string? estado = null);
    Task<ServiceResult<object>> GetByBarberoAsync(int barberoId);
    Task<ServiceResult<object>> GetAbonosAsync(int barberoId, int page, int pageSize);
    Task<ServiceResult<object>> GetAbonosByCicloAsync(int cicloId, int page, int pageSize);
    Task<ServiceResult<object>> GetAllAbonosByBarberoAsync(int barberoId, int page, int pageSize);
    Task<ServiceResult<object>> RegistrarAbonoAsync(int barberoId, AbonoInput input);
Task<ServiceResult<object>> GetOrCreateAsync(int barberoId);
    Task<ServiceResult<object>> ExtenderPlazoAsync(int barberoId, ExtenderPlazoInput input);
    Task<ServiceResult<object>> NuevoCicloAsync(int barberoId, NuevoCicloInput input);
    Task<ServiceResult<object>> SubirLimiteAsync(int barberoId, SubirLimiteInput input);
    Task RecalcularEstadosVencidosAsync(CancellationToken ct = default);
}
