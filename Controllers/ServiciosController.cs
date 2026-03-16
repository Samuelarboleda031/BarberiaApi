using BarberiaApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BarberiaApi.Services;
using BarberiaApi.Helpers;
using System.IO;
using System;


namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiciosController : ControllerBase
    {
        private readonly BarberiaContext _context;
        private readonly IPhotoService _photoService;

        public ServiciosController(BarberiaContext context, IPhotoService photoService)
        {
            _context = context;
            _photoService = photoService;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            var q = _context.Servicios.AsQueryable();
            var totalCount = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { items, totalCount, page, pageSize, totalPages });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Servicio>> GetById(int id)
        {
            // Busca el servicio sin importar el estado
            var servicio = await _context.Servicios
                .Include(s => s.DetallePaquetes)
                .FirstOrDefaultAsync(s => s.Id == id);

            // Solo falla si realmente NO existe en la BD
            if (servicio == null) return NotFound();
            return Ok(servicio);
        }

        [HttpPost("{id}/imagen")]
        [RequestSizeLimit(15728640)]
        public async Task<ActionResult<object>> SubirImagen(int id, IFormFile imagen)
        {
            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == id);
            if (servicio == null) return NotFound();
            if (imagen == null || imagen.Length == 0) return BadRequest("Imagen requerida");
            if (string.IsNullOrWhiteSpace(imagen.ContentType) || !imagen.ContentType.StartsWith("image/")) return BadRequest("Content-Type inválido");
            var res = await _photoService.AddPhotoAsync(imagen);
            if (res.Error != null) return BadRequest(res.Error.Message);
            var url = res.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(url)) return BadRequest("Error al subir");
            servicio.Imagen = url;
            await _context.SaveChangesAsync();
            return Ok(new { url, publicId = res.PublicId });
        }

        [HttpDelete("{id}/imagen")]
        public async Task<ActionResult<object>> EliminarImagen(int id, [FromQuery] bool borrarCloud = true)
        {
            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == id);
            if (servicio == null) return NotFound();
            var url = servicio.Imagen;
            if (string.IsNullOrWhiteSpace(url)) return Ok(new { eliminado = false });
            string publicId = borrarCloud ? ExtraerPublicIdDesdeUrl(url) ?? "" : "";
            if (borrarCloud && !string.IsNullOrWhiteSpace(publicId)) await _photoService.DeletePhotoAsync(publicId);
            servicio.Imagen = null;
            await _context.SaveChangesAsync();
            return Ok(new { eliminado = true, publicId });
        }

        private static string? ExtraerPublicIdDesdeUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var marker = "/upload/";
                var idx = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return null;
                var after = path[(idx + marker.Length)..].Trim('/');
                var segments = after.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 0) return null;
                var start = 0;
                if (segments[0].Length > 1 && segments[0][0] == 'v' && long.TryParse(segments[0][1..], out _)) start = 1;
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

        [HttpPost]
        public async Task<ActionResult<Servicio>> Create([FromBody] Servicio? servicio)
        {
            if (servicio == null)
                return BadRequest("El objeto servicio es requerido");

            if (string.IsNullOrWhiteSpace(servicio.Nombre))
                return BadRequest("El nombre del servicio es requerido");

            if (servicio.Precio <= 0)
                return BadRequest("El precio debe ser mayor a cero");

            // Validar URL de imagen usando el helper estandarizado
            if (!BarberiaApi.Helpers.ValidationHelper.ValidarUrlImagen(servicio.Imagen, out var imgError))
            {
                return BadRequest(imgError);
            }

            servicio.Id = 0;
            servicio.Estado = true;

            _context.Servicios.Add(servicio);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = servicio.Id }, servicio);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Servicio servicio)
        {
            if (id != servicio.Id) return BadRequest();

            // Busca el servicio existente sin importar el estado
            var servicioExistente = await _context.Servicios.FindAsync(id);
            // Solo falla si realmente NO existe en la BD
            if (servicioExistente == null) return NotFound();

            // Validar URL de imagen usando el helper estandarizado
            if (!BarberiaApi.Helpers.ValidationHelper.ValidarUrlImagen(servicio.Imagen, out var imgErrorUpdate))
            {
                return BadRequest(imgErrorUpdate);
            }

            _context.Entry(servicioExistente).CurrentValues.SetValues(servicio);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Servicios.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult<CambioEstadoResponse<Servicio>>> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        {
            var servicio = await _context.Servicios
                .Include(s => s.DetallePaquetes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (servicio == null) return NotFound();

            // Actualizar solo el estado
            servicio.Estado = input.estado;
            await _context.SaveChangesAsync();

            var response = new CambioEstadoResponse<Servicio>
            {
                entidad = servicio,
                mensaje = input.estado ? "Servicio activado exitosamente" : "Servicio desactivado exitosamente",
                exitoso = true
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Busca el servicio sin importar el estado
            var servicio = await _context.Servicios
                .Include(s => s.Agendamientos)
                .Include(s => s.DetalleVenta)
                .Include(s => s.DetallePaquetes)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            // Solo falla si realmente NO existe en la BD
            if (servicio == null) return NotFound();

            // Verificar si tiene agendamientos activos, ventas o está en paquetes
            bool tieneAgendamientosActivos = servicio.Agendamientos.Any(a => a.Estado != "Cancelada");
            bool tieneVentas = servicio.DetalleVenta.Any();
            bool estaEnPaquetes = servicio.DetallePaquetes.Any();

            if (tieneAgendamientosActivos || tieneVentas || estaEnPaquetes)
            {
                // Soft Delete: Cambia el estado a false
                servicio.Estado = false;
                await _context.SaveChangesAsync();
                return Ok(new { 
                    message = "Servicio desactivado (borrado lógico por tener registros asociados)", 
                    eliminado = true, 
                    fisico = false,
                    motivo = tieneAgendamientosActivos ? "Agendamientos activos" : 
                            tieneVentas ? "Ventas registradas" : "Incluido en paquetes"
                });
            }

            // Borrado Físico: No tiene registros críticos
            _context.Servicios.Remove(servicio);
            await _context.SaveChangesAsync();
            return Ok(new { 
                message = "Servicio eliminado físicamente de la base de datos", 
                eliminado = true, 
                fisico = true 
            });
        }
    }
}
