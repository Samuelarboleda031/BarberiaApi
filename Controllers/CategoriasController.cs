using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriasController : ControllerBase
    {
        private readonly ICategoriaService _categoriaService;
        public CategoriasController(ICategoriaService categoriaService) => _categoriaService = categoriaService;

        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        { var r = await _categoriaService.GetAllAsync(page, pageSize, q); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        { var r = await _categoriaService.GetByIdAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Categoria? categoria)
        { var r = await _categoriaService.CreateAsync(categoria!); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Categoria categoria)
        { var r = await _categoriaService.UpdateAsync(id, categoria); return r.Success ? NoContent() : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpPut("{id}/estado")]
        public async Task<ActionResult> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        { var r = await _categoriaService.CambiarEstadoAsync(id, input); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        { var r = await _categoriaService.DeleteAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }
    }
}
