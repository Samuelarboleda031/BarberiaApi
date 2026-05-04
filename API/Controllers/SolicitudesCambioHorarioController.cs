using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SolicitudesCambioHorarioController : ControllerBase
    {
        private readonly ISolicitudCambioHorarioService _service;

        public SolicitudesCambioHorarioController(ISolicitudCambioHorarioService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll(
            [FromQuery] string? estado = null,
            [FromQuery] int? barberoId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var r = await _service.GetAllAsync(estado, barberoId, page, pageSize);
            return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var r = await _service.GetByIdAsync(id);
            return r.Success ? Ok(r.Data) : (r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error));
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] SolicitudCambioHorarioCreateInput input)
        {
            var r = await _service.CreateAsync(input);
            return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error);
        }

        [HttpPost("{id}/aprobar")]
        public async Task<ActionResult> Aprobar(int id, [FromQuery] int usuarioId)
        {
            var r = await _service.AprobarAsync(id, usuarioId);
            return r.Success ? Ok(r.Data) : (r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error));
        }

        [HttpPost("{id}/rechazar")]
        public async Task<ActionResult> Rechazar(int id, [FromQuery] int usuarioId, [FromBody] SolicitudCambioHorarioRechazarInput input)
        {
            var r = await _service.RechazarAsync(id, usuarioId, input);
            return r.Success ? Ok(r.Data) : (r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error));
        }

        [HttpPost("{id}/responder")]
        public async Task<ActionResult> Responder(int id, [FromBody] SolicitudCambioHorarioRespuestaInput input)
        {
            var r = await _service.ResponderSugerenciaAsync(id, input);
            return r.Success ? Ok(r.Data) : (r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error));
        }
    }
}
