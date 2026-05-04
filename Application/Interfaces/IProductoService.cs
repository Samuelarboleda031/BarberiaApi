using BarberiaApi.Application.DTOs;
using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Application.Interfaces;

public interface IProductoService
{
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetStockBajoAsync(int page, int pageSize, string? q);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> CreateAsync(Producto producto);
    Task<ServiceResult<object>> UpdateAsync(int id, Producto producto);
    Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input);
    Task<ServiceResult<object>> TransferirStockAsync(int id, TransferirStockInput input);
    Task<ServiceResult<object>> DeleteAsync(int id);
    Task<ServiceResult<object>> GetPrecioCompraPromedioAsync(int id);
}
