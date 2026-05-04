using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EntregasInsumosController : ControllerBase
    {
        private readonly IEntregaInsumoService _entregaInsumoService;

        public EntregasInsumosController(IEntregaInsumoService entregaInsumoService)
        {
            _entregaInsumoService = entregaInsumoService;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            var result = await _entregaInsumoService.GetAllAsync(page, pageSize, q);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, result.Error);
        }

        [HttpGet("devoluciones")]
        public async Task<ActionResult<object>> GetDevoluciones([FromQuery] int? barberoId, [FromQuery] int? entregaId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            var result = await _entregaInsumoService.GetDevolucionesAsync(barberoId, entregaId, desde, hasta, page, pageSize, q);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, result.Error);
        }

        [HttpGet("devoluciones/resumen")]
        public async Task<ActionResult<object>> GetDevolucionesResumen([FromQuery] int? barberoId, [FromQuery] int? entregaId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var result = await _entregaInsumoService.GetDevolucionesResumenAsync(barberoId, entregaId, desde, hasta);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, result.Error);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EntregasInsumo>> GetById(int id)
        {
            var result = await _entregaInsumoService.GetByIdAsync(id);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : BadRequest(result.Error);
            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<ActionResult<EntregasInsumo>> Create([FromBody] EntregaInput input)
        {
            var result = await _entregaInsumoService.CreateAsync(input);
            if (!result.Success) return StatusCode(result.StatusCode, result.Error);
            return CreatedAtAction(nameof(GetById), new { id = ((dynamic)result.Data!).Id }, result.Data);
        }

        [HttpPut("{id}/estado")]
        public async Task<ActionResult<CambioEstadoResponse<EntregasInsumo>>> CambiarEstado(int id, [FromBody] CambioEstadoInput input)
        {
            var result = await _entregaInsumoService.CambiarEstadoAsync(id, input);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpPost("{id}/anular")]
        public async Task<ActionResult> Anular(int id)
        {
            var input = new CambioEstadoInput { estado = "Anulada" };
            var result = await _entregaInsumoService.CambiarEstadoAsync(id, input);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }
    }
}
