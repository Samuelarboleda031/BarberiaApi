using BarberiaApi.Services;
using BarberiaApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IPhotoService _photoService;
        private readonly BarberiaContext _context;

        public ImagesController(IPhotoService photoService, BarberiaContext context)
        {
            _photoService = photoService;
            _context = context;
        }

        [RequestSizeLimit(15728640)]
        [HttpPost("subir")]
        public async Task<ActionResult<object>> SubirImagen(
            IFormFile imagen,
            [FromForm] int? productoId,
            [FromForm] int? usuarioId)
        {
            if (imagen == null)
                return BadRequest("No se envió ninguna imagen");

            if (string.IsNullOrWhiteSpace(imagen.ContentType) || !imagen.ContentType.StartsWith("image/"))
                return BadRequest("El Content-Type debe ser una imagen.");

            var result = await _photoService.AddPhotoAsync(imagen);

            if (result.Error != null)
                return BadRequest(result.Error.Message);

            var url = result.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("Error al subir la imagen");

            if (productoId.HasValue)
            {
                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == productoId.Value);
                if (producto == null) return NotFound($"Producto {productoId.Value} no encontrado");

                producto.ImagenProduc = url;
                await _context.SaveChangesAsync();
            }
            else if (usuarioId.HasValue)
            {
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId.Value);
                if (usuario == null) return NotFound($"Usuario {usuarioId.Value} no encontrado");

                usuario.FotoPerfil = url;
                usuario.FechaModificacion = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                url,
                publicId = result.PublicId,
                productoId = productoId,
                usuarioId = usuarioId
            });
        }

        [HttpDelete("producto/{id}")]
        public async Task<ActionResult<object>> EliminarImagenProducto(int id, [FromQuery] bool borrarCloud = true)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();

            var url = producto.ImagenProduc;
            if (string.IsNullOrWhiteSpace(url))
            {
                return Ok(new { eliminado = false, mensaje = "El producto no tiene imagen" });
            }

            string? publicId = borrarCloud ? ExtraerPublicIdDesdeUrl(url) : null;
            if (borrarCloud && !string.IsNullOrWhiteSpace(publicId))
            {
                var resp = await _photoService.DeletePhotoAsync(publicId);
            }

            producto.ImagenProduc = null;
            await _context.SaveChangesAsync();
            return Ok(new { eliminado = true, publicId = publicId });
        }

        [HttpDelete("usuario/{id}")]
        public async Task<ActionResult<object>> EliminarImagenUsuario(int id, [FromQuery] bool borrarCloud = true)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null) return NotFound();

            var url = usuario.FotoPerfil;
            if (string.IsNullOrWhiteSpace(url))
            {
                return Ok(new { eliminado = false, mensaje = "El usuario no tiene foto" });
            }

            string? publicId = borrarCloud ? ExtraerPublicIdDesdeUrl(url) : null;
            if (borrarCloud && !string.IsNullOrWhiteSpace(publicId))
            {
                var resp = await _photoService.DeletePhotoAsync(publicId);
            }

            usuario.FotoPerfil = null;
            usuario.FechaModificacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { eliminado = true, publicId = publicId });
        }

        private static string? ExtraerPublicIdDesdeUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath; // /<cloudinary>/image/upload/.../folder/file.ext
                var marker = "/upload/";
                var idx = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return null;
                var after = path.Substring(idx + marker.Length).Trim('/');
                var segments = after.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 0) return null;
                int start = 0;
                if (segments[0].Length > 1 && segments[0][0] == 'v' && long.TryParse(segments[0].Substring(1), out _))
                    start = 1; // omitir segmento de versión v12345
                if (start >= segments.Length) return null;
                var last = segments[^1];
                var nameNoExt = Path.GetFileNameWithoutExtension(last);
                var leading = segments.Length - start > 1 ? string.Join('/', segments[start..^1]) + "/" : string.Empty;
                return leading + nameNoExt;
            }
            catch
            {
                return null;
            }
        }
    }
}
