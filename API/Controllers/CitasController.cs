using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [System.Obsolete("CitasController es legacy. Usar AgendamientosController. Será removido en próximas versiones.")]
    public class CitasController : ControllerBase
    {
        private readonly ICitaService _citaService;

        public CitasController(ICitaService citaService)
        {
            _citaService = citaService;
        }

        [HttpGet]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<IEnumerable<CitaFrontend>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 100, [FromQuery] string? q = null)
        {
            var result = await _citaService.GetAllAsync(page, pageSize, q);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, result.Error);
        }

        [HttpGet("{id}")]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<CitaFrontend>> GetById(int id)
        {
            var result = await _citaService.GetByIdAsync(id);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : BadRequest(result.Error);
            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<ActionResult<CitaFrontend>> Create([FromBody] CitaInputFrontend input)
        {
            var result = await _citaService.CreateAsync(input);
            if (!result.Success) return StatusCode(result.StatusCode, result.Error);
            return CreatedAtAction(nameof(GetById), new { id = ((dynamic)result.Data!).id }, result.Data);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CitaFrontend>> Update(int id, [FromBody] CitaInputFrontend input)
        {
            var result = await _citaService.UpdateAsync(id, input);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult<CitaFrontend>> CambiarEstado(int id, [FromBody] CambioEstadoInput input)
        {
            var result = await _citaService.CambiarEstadoAsync(id, input);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _citaService.DeleteAsync(id);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }
    }
}
