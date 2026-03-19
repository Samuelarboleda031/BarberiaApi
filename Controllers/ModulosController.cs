using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModulosController : ControllerBase
    {
        private readonly IModuloService _moduloService;
        public ModulosController(IModuloService moduloService) => _moduloService = moduloService;

        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        { var r = await _moduloService.GetAllAsync(page, pageSize, q); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        { var r = await _moduloService.GetByIdAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Modulos modulo)
        { var r = await _moduloService.CreateAsync(modulo); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Modulos modulo)
        { var r = await _moduloService.UpdateAsync(id, modulo); return r.Success ? NoContent() : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        { var r = await _moduloService.DeleteAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }
    }
}
