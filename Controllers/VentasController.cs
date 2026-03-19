using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly IVentaService _ventaService;

        public VentasController(IVentaService ventaService)
        {
            _ventaService = ventaService;
        }

        [HttpGet]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            var result = await _ventaService.GetAllAsync(page, pageSize, q);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, result.Error);
        }

        [HttpGet("{id}")]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> GetById(int id)
        {
            var result = await _ventaService.GetByIdAsync(id);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : BadRequest(result.Error);
            return Ok(result.Data);
        }

        [HttpGet("por-agendamiento/{agendamientoId}")]
        public async Task<ActionResult> GetByAgendamiento(int agendamientoId)
        {
            var result = await _ventaService.GetByAgendamientoAsync(agendamientoId);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : BadRequest(result.Error);
            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] VentaInput input)
        {
            var result = await _ventaService.CreateAsync(input);
            if (!result.Success) return StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpPut("{id}/anular")]
        public async Task<ActionResult> AnularVenta(int id)
        {
            var result = await _ventaService.AnularAsync(id);
            if (!result.Success) return StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }
    }
}
