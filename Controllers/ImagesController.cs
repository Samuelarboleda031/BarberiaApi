using BarberiaApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;
        public ImagesController(IImageService imageService) => _imageService = imageService;

        [RequestSizeLimit(15728640)]
        [HttpPost("subir")]
        public async Task<ActionResult> SubirImagen(IFormFile imagen, [FromForm] int? productoId, [FromForm] int? usuarioId)
        { var r = await _imageService.SubirImagenAsync(imagen, productoId, usuarioId); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound(r.Error) : BadRequest(r.Error); }

        [HttpDelete("producto/{id}")]
        public async Task<ActionResult> EliminarImagenProducto(int id, [FromQuery] bool borrarCloud = true)
        { var r = await _imageService.EliminarImagenProductoAsync(id, borrarCloud); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpDelete("usuario/{id}")]
        public async Task<ActionResult> EliminarImagenUsuario(int id, [FromQuery] bool borrarCloud = true)
        { var r = await _imageService.EliminarImagenUsuarioAsync(id, borrarCloud); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }
    }
}
