using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BarberosController : ControllerBase
    {
        private readonly IBarberoService _barberoService;
        public BarberosController(IBarberoService barberoService) => _barberoService = barberoService;

        [HttpGet] [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        { var r = await _barberoService.GetAllAsync(page, pageSize, q); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpGet("{id}")] [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> GetById(int id)
        { var r = await _barberoService.GetByIdAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] BarberoInput input)
        { var r = await _barberoService.CreateAsync(input); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BarberoInput input)
        { var r = await _barberoService.UpdateAsync(id, input); return r.Success ? NoContent() : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        { var r = await _barberoService.CambiarEstadoAsync(id, input); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        { var r = await _barberoService.DeleteAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }
    }
}
