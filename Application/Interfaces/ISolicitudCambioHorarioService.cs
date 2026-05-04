using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Interfaces;

public interface ISolicitudCambioHorarioService
{
    Task<ServiceResult<object>> GetAllAsync(string? estado, int? barberoId, int page, int pageSize);
    Task<ServiceResult<object>> GetByIdAsync(int id);
    Task<ServiceResult<object>> CreateAsync(SolicitudCambioHorarioCreateInput input);
    Task<ServiceResult<object>> AprobarAsync(int id, int usuarioId);
    Task<ServiceResult<object>> RechazarAsync(int id, int usuarioId, SolicitudCambioHorarioRechazarInput input);
    Task<ServiceResult<object>> ResponderSugerenciaAsync(int id, SolicitudCambioHorarioRespuestaInput input);
}
