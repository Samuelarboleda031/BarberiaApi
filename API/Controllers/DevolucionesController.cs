using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevolucionesController : ControllerBase
    {
        private readonly IDevolucionService _devolucionService;

        public DevolucionesController(IDevolucionService devolucionService)
        {
            _devolucionService = devolucionService;
        }

        [HttpPost("insumos/barbero")]
        public async Task<ActionResult<object>> DevolucionInsumosBarbero([FromBody] EntregaInput input)
        {
            var result = await _devolucionService.DevolucionInsumosBarberoAsync(input);
            if (!result.Success) return StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpGet]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult<object>> GetAll([FromQuery] int? barberoId, [FromQuery] int? clienteId, [FromQuery] int? productoId, [FromQuery] int? entregaId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            var result = await _devolucionService.GetAllAsync(barberoId, clienteId, productoId, entregaId, desde, hasta, page, pageSize, q);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, result.Error);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            var result = await _devolucionService.GetByIdAsync(id);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : BadRequest(result.Error);
            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] DevolucionInput input)
        {
            var result = await _devolucionService.CreateAsync(input);
            if (!result.Success) return StatusCode(result.StatusCode, result.Error);
            return CreatedAtAction(nameof(GetById), new { id = ((dynamic)result.Data!).Id }, result.Data);
        }

        [HttpPost("lote")]
        public async Task<ActionResult> CreateBatch([FromBody] DevolucionBatchInput input)
        {
            var result = await _devolucionService.CreateBatchAsync(input);
            if (!result.Success) return StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult<object>> CambiarEstado(int id, [FromBody] CambioEstadoInput input)
        {
            var result = await _devolucionService.CambiarEstadoAsync(id, input);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DevolucionUpdateInput input)
        {
            var result = await _devolucionService.UpdateAsync(id, input);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : StatusCode(result.StatusCode, result.Error);
            return NoContent();
        }
    }
}
