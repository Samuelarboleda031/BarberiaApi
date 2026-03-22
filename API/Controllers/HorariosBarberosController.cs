using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HorariosBarberosController : ControllerBase
    {
        private readonly IHorarioService _horarioService;

        public HorariosBarberosController(IHorarioService horarioService)
        {
            _horarioService = horarioService;
        }

        [HttpGet]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            var result = await _horarioService.GetAllAsync(page, pageSize, q);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, result.Error);
        }

        [HttpGet("{id}")]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<HorariosBarbero>> GetById(int id)
        {
            var result = await _horarioService.GetByIdAsync(id);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : BadRequest(result.Error);
            return Ok(result.Data);
        }

        [HttpGet("barbero/{barberoId}")]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<object>> GetByBarbero(int barberoId, [FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            var result = await _horarioService.GetByBarberoAsync(barberoId, page, pageSize, q);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, result.Error);
        }

        [HttpPost]
        public async Task<ActionResult<HorariosBarbero>> Create([FromBody] HorarioBarberoCreateInput input)
        {
            var result = await _horarioService.CreateAsync(input);
            if (!result.Success) return StatusCode(result.StatusCode, result.Error);
            return CreatedAtAction(nameof(GetById), new { id = ((dynamic)result.Data!).Id }, result.Data);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<HorariosBarbero>> Update(int id, [FromBody] HorarioBarberoUpdateInput input)
        {
            var result = await _horarioService.UpdateAsync(id, input);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _horarioService.DeleteAsync(id);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpPost("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambioEstadoHorarioInput input)
        {
            var result = await _horarioService.CambiarEstadoAsync(id, input);
            if (!result.Success)
            {
                return result.StatusCode switch
                {
                    401 => Unauthorized(result.Error),
                    403 => Forbid(),
                    404 => NotFound(),
                    _ => StatusCode(result.StatusCode, result.Error)
                };
            }
            return Ok(result.Data);
        }

        [HttpGet("disponibles/{fecha}")]
        public async Task<ActionResult<IEnumerable<object>>> GetDisponibles(string fecha)
        {
            var result = await _horarioService.GetDisponiblesAsync(fecha);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, result.Error);
        }

        [HttpPost("barbero/{barberoId}/cancelar-dia")]
        public async Task<IActionResult> CancelarDiaPorBarbero(int barberoId, [FromBody] CambioEstadoHorarioInput input)
        {
            var result = await _horarioService.CancelarDiaPorBarberoAsync(barberoId, input);
            if (!result.Success)
            {
                return result.StatusCode switch
                {
                    401 => Unauthorized(result.Error),
                    403 => Forbid(),
                    404 => NotFound(),
                    _ => StatusCode(result.StatusCode, result.Error)
                };
            }
            return Ok(result.Data);
        }
    }
}
