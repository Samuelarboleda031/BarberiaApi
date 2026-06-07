using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IImageService _imageService;
        private readonly BarberiaContext _context;

        public UsuariosController(IUsuarioService usuarioService, IImageService imageService, BarberiaContext context)
        {
            _usuarioService = usuarioService;
            _imageService = imageService;
            _context = context;
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
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> SubirFoto(int id, [FromForm] IFormFile imagen)
        { var r = await _imageService.SubirFotoUsuarioAsync(id, imagen); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpDelete("{id}/foto")]
        public async Task<ActionResult> EliminarFoto(int id, [FromQuery] bool borrarCloud = true)
        { var r = await _imageService.EliminarFotoUsuarioAsync(id, borrarCloud); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error); }

        [HttpPost]
        [AllowAnonymous] // Registro público
        public async Task<ActionResult> Create([FromBody] UsuarioInput input)
        { var r = await _usuarioService.CreateAsync(input); if (!r.Success) return StatusCode(r.StatusCode, r.Error); return CreatedAtAction(nameof(GetById), new { id = ((dynamic)r.Data!).Id }, r.Data); }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UsuarioInput input)
        { var r = await _usuarioService.UpdateAsync(id, input); return r.Success ? NoContent() : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        { var r = await _usuarioService.CambiarEstadoAsync(id, input); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,admin,super_admin")]
        public async Task<IActionResult> Delete(int id)
        { var r = await _usuarioService.DeleteAsync(id); return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : StatusCode(r.StatusCode, r.Error); }

        /// <summary>
        /// Actualiza el hash de contraseña en SQL para el usuario autenticado.
        /// Se llama después de un reset de contraseña vía Firebase para mantener sincronía.
        /// </summary>
        [HttpPut("contrasena-propia")]
        [Authorize]
        public async Task<IActionResult> ActualizarContrasenaPropia([FromBody] ActualizarContrasenaInput input)
        {
            if (string.IsNullOrWhiteSpace(input.NuevaContrasena) || input.NuevaContrasena.Length < 6)
                return BadRequest(new { error = "La contraseña debe tener al menos 6 caracteres." });

            var email = User.FindFirst("email")?.Value
                        ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized(new { error = "No se pudo identificar al usuario." });

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            if (usuario == null)
                return NotFound(new { error = "Usuario no encontrado en el sistema." });

            usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(input.NuevaContrasena);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Contraseña actualizada correctamente." });
        }

        [HttpPost("migrar-contrasenas")]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> MigrarContrasenas()
        {
            var usuarios = await _context.Usuarios.ToListAsync();
            int migrados = 0;
            int omitidos = 0;

            foreach (var u in usuarios)
            {
                // Los hashes de BCrypt siempre comienzan con $2a$ o $2b$
                bool yaHasheada = !string.IsNullOrWhiteSpace(u.Contrasena) &&
                                  (u.Contrasena.StartsWith("$2a$") || u.Contrasena.StartsWith("$2b$"));

                if (yaHasheada)
                {
                    omitidos++;
                    continue;
                }

                // Si está vacía o en texto plano, hashear
                var valorAHashear = string.IsNullOrWhiteSpace(u.Contrasena)
                    ? Guid.NewGuid().ToString()
                    : u.Contrasena;

                u.Contrasena = BCrypt.Net.BCrypt.HashPassword(valorAHashear);
                migrados++;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Migración completada",
                migrados,
                omitidos,
                total = usuarios.Count
            });
        }
    }
}
