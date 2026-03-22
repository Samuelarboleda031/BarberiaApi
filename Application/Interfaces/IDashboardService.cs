using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface IDashboardService
{
    Task<ServiceResult<object>> GetDashboardAsync();
}
