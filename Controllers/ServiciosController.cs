using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiciosController : ControllerBase
    {
        private readonly IServicioService _servicioService;
        private readonly IImageService _imageService;
        public ServiciosController(IServicioService servicioService, IImageService imageService) { _servicioService = servicioService; _imageService = imageService; }

        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        { var r = await _servicioService.GetAllAsync(page, pageSize, q); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        { var r = await _servicioService.GetByIdAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpPost("{id}/imagen")] [RequestSizeLimit(15728640)]
        public async Task<ActionResult> SubirImagen(int id, IFormFile imagen)
        { var r = await _imageService.SubirImagenServicioAsync(id, imagen); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpDelete("{id}/imagen")]
        public async Task<ActionResult> EliminarImagen(int id, [FromQuery] bool borrarCloud = true)
        { var r = await _imageService.EliminarImagenServicioAsync(id, borrarCloud); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Servicio? servicio)
        { var r = await _servicioService.CreateAsync(servicio!); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Servicio servicio)
        { var r = await _servicioService.UpdateAsync(id, servicio); return r.Success ? NoContent() : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        { var r = await _servicioService.CambiarEstadoAsync(id, input); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        { var r = await _servicioService.DeleteAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }
    }
}
