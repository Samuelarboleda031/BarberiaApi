using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaquetesController : ControllerBase
    {
        private readonly IPaqueteService _paqueteService;
        public PaquetesController(IPaqueteService paqueteService) => _paqueteService = paqueteService;

        [HttpGet] [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        { var r = await _paqueteService.GetAllAsync(page, pageSize, q); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpGet("{id}")] [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> GetById(int id)
        { var r = await _paqueteService.GetByIdAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] PaqueteInput input)
        { var r = await _paqueteService.CreateAsync(input); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpPost("completo")]
        public async Task<ActionResult> CreateCompleto([FromBody] PaqueteConDetallesInput input)
        { var r = await _paqueteService.CreateCompletoAsync(input); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Paquete paquete)
        { var r = await _paqueteService.UpdateAsync(id, paquete); return r.Success ? NoContent() : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        public class PaqueteDetallesUpdateInput { public List<DetallePaqueteSimpleInput> Detalles { get; set; } = new(); }

        [HttpPut("{id}/detalles")]
        public async Task<ActionResult> UpdateDetalles(int id, [FromBody] PaqueteDetallesUpdateInput input)
        { var r = await _paqueteService.UpdateDetallesAsync(id, input?.Detalles ?? new()); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpPut("{id}/estado")]
        public async Task<ActionResult> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        { var r = await _paqueteService.CambiarEstadoAsync(id, input); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        { var r = await _paqueteService.DeleteAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }
    }
}
