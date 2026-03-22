using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgendamientosController : ControllerBase
    {
        private readonly IAgendamientoService _agendamientoService;

        public AgendamientosController(IAgendamientoService agendamientoService)
        {
            _agendamientoService = agendamientoService;
        }

        [HttpGet]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 100, [FromQuery] string? q = null)
        {
            var result = await _agendamientoService.GetAllAsync(page, pageSize, q);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, result.Error);
        }

        [HttpGet("{id}")]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<AgendamientoDTO>> GetById(int id)
        {
            var result = await _agendamientoService.GetByIdAsync(id);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : BadRequest(result.Error);
            return Ok(result.Data);
        }

        [HttpGet("barbero/{barberoId}/{fecha}")]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<IEnumerable<AgendamientoDTO>>> GetByBarberoYFecha(int barberoId, DateTime fecha)
        {
            var result = await _agendamientoService.GetByBarberoYFechaAsync(barberoId, fecha);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, result.Error);
        }

        [HttpGet("cliente/{clienteId}")]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<object>> GetByCliente(int clienteId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, [FromQuery] string? q = null)
        {
            var result = await _agendamientoService.GetByClienteAsync(clienteId, page, pageSize, q);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, result.Error);
        }

        [HttpPost]
        public async Task<ActionResult<AgendamientoDTO>> Create([FromBody] AgendamientoInput input)
        {
            var result = await _agendamientoService.CreateAsync(input);
            if (!result.Success) return StatusCode(result.StatusCode, result.Error);
            return CreatedAtAction(nameof(GetById), new { id = ((dynamic)result.Data!).Id }, result.Data);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AgendamientoInput input)
        {
            var result = await _agendamientoService.UpdateAsync(id, input);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : StatusCode(result.StatusCode, result.Error);
            return NoContent();
        }

        [HttpPatch("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambioEstadoInput input)
        {
            var result = await _agendamientoService.CambiarEstadoAsync(id, input);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _agendamientoService.DeleteAsync(id);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }
    }
}
