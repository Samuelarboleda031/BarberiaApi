using BarberiaApi.Application.DTOs;
using BarberiaApi.Domain.Entities;

namespace BarberiaApi.Application.Interfaces;

public interface IAgendamientoService
{
    Task EnviarNotificacionesCreacionAsync(Agendamiento agendamiento);
    Task EnviarNotificacionesCancelacionAsync(Agendamiento agendamiento, string motivo);
    Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q, bool? estaSemana);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> GetByBarberoYFechaAsync(int barberoId, DateTime fecha);
    Task<ServiceResult<object>> GetByClienteAsync(int clienteId, int page, int pageSize, string? q, bool? estaSemana);
    Task<ServiceResult<object>> CreateAsync(AgendamientoInput input);
    Task<ServiceResult<object>> UpdateAsync(int id, AgendamientoInput input);
    Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoInput input);
    Task<ServiceResult<object>> DeleteAsync(int id);
    Task<ServiceResult<object>> CompletarParcialmenteAsync(int id, CompletarParcialmenteRequest request);
    Task<ServiceResult<object>> GetPorTerminarAsync();
}
