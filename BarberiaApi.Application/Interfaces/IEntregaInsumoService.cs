using BarberiaApi.Application.DTOs;
using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Application.Interfaces;

public interface IEntregaInsumoService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetDevolucionesAsync(int? barberoId, int? entregaId, DateTime? desde, DateTime? hasta, int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetDevolucionesResumenAsync(int? barberoId, int? entregaId, DateTime? desde, DateTime? hasta);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> CreateAsync(EntregaInput input);
    Task<ServiceResult<object>> UpdateAsync(int id, EntregasInsumo entrega);
    Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoInput input);
}
