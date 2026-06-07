using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using BarberiaApi.Infrastructure.Services;

namespace BarberiaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    // ============================================
    // CARGAR IMAGENES
    // ============================================
    public class UploadController : ControllerBase
    {
        private readonly IPhotoService _photoService;
        private readonly BarberiaApi.Infrastructure.Helpers.CloudinarySettings _cloudinarySettings;
        private readonly IHostEnvironment _env;

        public UploadController(IPhotoService photoService, Microsoft.Extensions.Options.IOptions<BarberiaApi.Infrastructure.Helpers.CloudinarySettings> options, IHostEnvironment env)
        {
            _photoService = photoService;
            _cloudinarySettings = options.Value;
            _env = env;
        }

        [RequestSizeLimit(15728640)]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha proporcionado ningún archivo.");

            if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/"))
                return BadRequest("El Content-Type debe ser una imagen.");

            // Validar extensión
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !IsImageExtension(extension))
            {
                return BadRequest("Formato de archivo no válido. Solo se permiten imágenes (jpg, jpeg, png, gif, webp).");
            }

            // BE-B5: validar la firma binaria (magic bytes), no solo la extensión/ContentType
            // (ambos son falsificables). Evita subir un ejecutable renombrado a .jpg.
            if (!await TieneFirmaDeImagenValida(file))
            {
                return BadRequest("El contenido del archivo no corresponde a una imagen válida.");
            }

            // Subir usando PhotoService (Cloudinary)
            var result = await _photoService.AddPhotoAsync(file);

            if (result.Error != null)
                return BadRequest(result.Error.Message);

            // Retornar objeto JSON con la url y el publicId
            return Ok(new
            {
                url = result.SecureUrl.AbsoluteUri,
                publicId = result.PublicId
            });
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            bool configured = !string.IsNullOrWhiteSpace(_cloudinarySettings.CloudName)
                              && !string.IsNullOrWhiteSpace(_cloudinarySettings.ApiKey)
                              && !string.IsNullOrWhiteSpace(_cloudinarySettings.ApiSecret);
            if (_env.IsProduction())
            {
                return Ok(new { configured });
            }
            else
            {
                return Ok(new
                {
                    configured,
                    cloudName = Mask(_cloudinarySettings.CloudName),
                    apiKey = Mask(_cloudinarySettings.ApiKey),
                    apiSecret = Mask(_cloudinarySettings.ApiSecret)
                });
            }
        }

        private static string Mask(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Length <= 4) return new string('*', value.Length);
            return $"{value.Substring(0, 2)}***{value.Substring(value.Length - 2)}";
        }

        private bool IsImageExtension(string extension)
        {
            return extension == ".jpg" ||
                   extension == ".jpeg" ||
                   extension == ".png" ||
                   extension == ".gif" ||
                   extension == ".webp";
        }

        // BE-B5: verifica los primeros bytes del archivo contra las firmas conocidas
        // de imagen (JPEG, PNG, GIF, WEBP). No confía en la extensión ni en el Content-Type.
        private static async Task<bool> TieneFirmaDeImagenValida(IFormFile file)
        {
            try
            {
                await using var stream = file.OpenReadStream();
                var header = new byte[12];
                var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
                if (read < 12) return false;

                // JPEG: FF D8 FF
                if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                    return true;
                // PNG: 89 50 4E 47
                if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                    return true;
                // GIF: 47 49 46 38 ("GIF8")
                if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
                    return true;
                // WEBP: "RIFF"...."WEBP"
                if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                    header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}



