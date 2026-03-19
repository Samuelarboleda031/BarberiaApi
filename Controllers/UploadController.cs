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
    }
}



