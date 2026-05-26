using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface IDevolucionService
{
    Task<ServiceResult<object>> GetAllAsync(int? clienteId, int? productoId, DateTime? desde, DateTime? hasta, int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> CreateAsync(DevolucionInput input);
    Task<ServiceResult<object>> CreateBatchAsync(DevolucionBatchInput input);
    Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoInput input);
    Task<ServiceResult<object>> AnularAsync(int id);
    Task<ServiceResult<object>> GetByClienteAsync(int clienteId, int page, int pageSize);
}
