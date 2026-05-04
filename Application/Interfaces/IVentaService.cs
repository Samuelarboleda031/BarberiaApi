using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface IVentaService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? searchTerm);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> GetByAgendamientoAsync(int agendamientoId);
    Task<ServiceResult<object>> GetByClienteAsync(int clienteId, int page, int pageSize);
    Task<ServiceResult<object>> CreateAsync(VentaInput input);
    Task<ServiceResult<object>> AnularAsync(int id);
}
