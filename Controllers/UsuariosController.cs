using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IImageService _imageService;

        public UsuariosController(IUsuarioService usuarioService, IImageService imageService)
        {
            _usuarioService = usuarioService;
            _imageService = imageService;
        }

        [HttpGet("analisis")] [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> Analisis([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        { var r = await _usuarioService.AnalisisAsync(page, pageSize); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpGet("{id}/analisis")] [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> AnalisisPorId(int id)
        { var r = await _usuarioService.AnalisisPorIdAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpGet] [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        { var r = await _usuarioService.GetAllAsync(page, pageSize, q); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpGet("{id}")] [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> GetById(int id)
        { var r = await _usuarioService.GetByIdAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpPost("{id}/foto")] [RequestSizeLimit(15728640)]
        public async Task<ActionResult> SubirFoto(int id, IFormFile imagen)
        { var r = await _imageService.SubirFotoUsuarioAsync(id, imagen); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpDelete("{id}/foto")]
        public async Task<ActionResult> EliminarFoto(int id, [FromQuery] bool borrarCloud = true)
        { var r = await _imageService.EliminarFotoUsuarioAsync(id, borrarCloud); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] UsuarioInput input)
        { var r = await _usuarioService.CreateAsync(input); if (!r.Success) return StatusCode(r.StatusCode, r.Error); return CreatedAtAction(nameof(GetById), new { id = ((dynamic)r.Data!).Id }, r.Data); }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UsuarioInput input)
        { var r = await _usuarioService.UpdateAsync(id, input); return r.Success ? NoContent() : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        { var r = await _usuarioService.CambiarEstadoAsync(id, input); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        { var r = await _usuarioService.DeleteAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }
    }
}
