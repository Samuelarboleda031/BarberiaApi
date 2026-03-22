using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface IDevolucionService
{
    Task<ServiceResult<object>> DevolucionInsumosBarberoAsync(EntregaInput input);
    Task<ServiceResult<object>> GetAllAsync(int? barberoId, int? clienteId, int? productoId, int? entregaId, DateTime? desde, DateTime? hasta, int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> CreateAsync(DevolucionInput input);
    Task<ServiceResult<object>> CreateBatchAsync(DevolucionBatchInput input);
    Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoInput input);
    Task<ServiceResult<object>> UpdateAsync(int id, DevolucionUpdateInput input);
}
