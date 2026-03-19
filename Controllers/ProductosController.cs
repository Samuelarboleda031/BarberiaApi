using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly IProductoService _productoService;
        private readonly IImageService _imageService;

        public ProductosController(IProductoService productoService, IImageService imageService)
        {
            _productoService = productoService;
            _imageService = imageService;
        }

        [HttpGet] [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        { var r = await _productoService.GetAllAsync(page, pageSize, q); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpPost("{id}/imagen")] [RequestSizeLimit(15728640)]
        public async Task<ActionResult> SubirImagen(int id, IFormFile imagen)
        { var r = await _imageService.SubirImagenProductoAsync(id, imagen); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpDelete("{id}/imagen")]
        public async Task<ActionResult> EliminarImagen(int id, [FromQuery] bool borrarCloud = true)
        { var r = await _imageService.EliminarImagenProductoDirectaAsync(id, borrarCloud); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpGet("stock-bajo")] [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> GetStockBajo([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? q = null)
        { var r = await _productoService.GetStockBajoAsync(page, pageSize, q); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpGet("{id}")] [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> GetById(int id)
        { var r = await _productoService.GetByIdAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Producto? producto)
        { var r = await _productoService.CreateAsync(producto!); if (!r.Success) return StatusCode(r.StatusCode, r.Error); return CreatedAtAction(nameof(GetById), new { id = ((dynamic)r.Data!).Id }, r.Data); }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Producto producto)
        { var r = await _productoService.UpdateAsync(id, producto); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }

        [HttpPost("{id}/transferir-stock")]
        public async Task<ActionResult> TransferirStock(int id, [FromBody] TransferirStockInput input)
        { var r = await _productoService.TransferirStockAsync(id, input); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }

        [HttpPut("{id}/estado")]
        public async Task<ActionResult> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        { var r = await _productoService.CambiarEstadoAsync(id, input); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        { var r = await _productoService.DeleteAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }
    }
}
