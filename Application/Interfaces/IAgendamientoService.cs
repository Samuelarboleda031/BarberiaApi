using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface IAgendamientoService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q, bool? estaSemana);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> GetByBarberoYFechaAsync(int barberoId, DateTime fecha);
    Task<ServiceResult<object>> GetByClienteAsync(int clienteId, int page, int pageSize, string? q, bool? estaSemana);
    Task<ServiceResult<object>> CreateAsync(AgendamientoInput input);
    Task<ServiceResult<object>> UpdateAsync(int id, AgendamientoInput input);
    Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoInput input);
    Task<ServiceResult<object>> DeleteAsync(int id);
}
