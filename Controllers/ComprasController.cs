using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComprasController : ControllerBase
    {
        private readonly ICompraService _compraService;

        public ComprasController(ICompraService compraService)
        {
            _compraService = compraService;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        {
            var result = await _compraService.GetAllAsync(page, pageSize, q);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, result.Error);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var result = await _compraService.GetByIdAsync(id);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : BadRequest(result.Error);
            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CompraInput input)
        {
            var result = await _compraService.CreateAsync(input);
            if (!result.Success) return StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _compraService.AnularAsync(id);
            if (!result.Success) return result.StatusCode == 404 ? NotFound() : StatusCode(result.StatusCode, result.Error);
            return NoContent();
        }
    }
}
