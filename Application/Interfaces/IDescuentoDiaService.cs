using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface IDescuentoDiaService
{
    /// <summary>Devuelve todos los descuentos como mapa { "yyyy-MM-dd": porcentaje }.</summary>
    Task<ServiceResult<object>> GetAllAsync();

    /// <summary>Crea o actualiza el descuento de un día. Si el porcentaje es 0, lo elimina.</summary>
    Task<ServiceResult<object>> UpsertAsync(DescuentoDiaInput input);

    /// <summary>Elimina el descuento de un día (fecha en formato "yyyy-MM-dd").</summary>
    Task<ServiceResult<object>> DeleteAsync(string fecha);
}
