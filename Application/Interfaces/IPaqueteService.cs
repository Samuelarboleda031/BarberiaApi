using BarberiaApi.Application.DTOs;
using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Application.Interfaces;

public interface IPaqueteService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> CreateAsync(PaqueteInput input);
    Task<ServiceResult<object>> CreateCompletoAsync(PaqueteConDetallesInput input);
    Task<ServiceResult<object>> UpdateAsync(int id, Paquete paquete);
    Task<ServiceResult<object>> UpdateDetallesAsync(int id, List<DetallePaqueteSimpleInput> detalles);
    Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input);
    Task<ServiceResult<object>> DeleteAsync(int id);
}

public class DetallePaqueteSimpleInput
{
    public int ServicioId { get; set; }
    public int Cantidad { get; set; }
}
