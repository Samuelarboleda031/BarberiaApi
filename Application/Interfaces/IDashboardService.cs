using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface IDashboardService
{
    Task<ServiceResult<object>> GetDashboardAsync();
    Task<ServiceResult<object>> GetGananciasAsync(string periodo, string barbero);
}
