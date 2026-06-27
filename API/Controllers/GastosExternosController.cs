using BarberiaApi.Application.Common;
using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GastosExternosController : ControllerBase
    {
        private readonly IGastoExternoService _service;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly BarberiaContext _context;

        public GastosExternosController(IGastoExternoService service, IDateTimeProvider dateTimeProvider, BarberiaContext context)
        {
            _service = service;
            _dateTimeProvider = dateTimeProvider;
            _context = context;
        }

        // ── POST /api/GastosExternos ──────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin,admin,super_admin")]
        public async Task<ActionResult> Create([FromBody] GastoExternoInput input)
        {
            // Obtener usuarioId del token o buscar por email para compatibilidad
            if (input.UsuarioId <= 0)
            {
                var email = User.FindFirst("email")?.Value
                            ?? User.FindFirst(ClaimTypes.Email)?.Value;

                int usuarioId = 0;

                // Intentar con NameIdentifier primero
                var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(usuarioIdStr, out var parsedId) && parsedId > 0)
                {
                    usuarioId = parsedId;
                }
                // Si no, buscar en BD por email
                else if (!string.IsNullOrWhiteSpace(email))
                {
                    var usuario = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Correo == email);
                    if (usuario != null)
                    {
                        usuarioId = usuario.Id;
                    }
                }

                if (usuarioId <= 0)
                {
                    return BadRequest("No se pudo identificar el usuario autenticado. Por favor, cierra sesión y vuelve a iniciar sesión.");
                }

                input.UsuarioId = usuarioId;
            }

            var r = await _service.CreateAsync(input);
            return r.Success ? CreatedAtAction(nameof(GetById), new { id = r.Data!.Id }, r.Data)
                             : StatusCode(r.StatusCode, r.Error);
        }

        // ── GET /api/GastosExternos/{id} ─────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetById(int id)
        {
            var r = await _service.GetByIdAsync(id);
            return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error);
        }

        // ── GET /api/GastosExternos/fecha/{fecha} ────────────────────────────
        [HttpGet("fecha/{fecha}")]
        public async Task<ActionResult> GetByDate(string fecha)
        {
            if (!DateOnly.TryParse(fecha, out var parsed))
                return BadRequest("Formato de fecha inválido. Use yyyy-MM-dd.");

            var r = await _service.GetByDateAsync(parsed);
            return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error);
        }

        // ── GET /api/GastosExternos/dia/actual ─────────────────────────────────────
        [HttpGet("dia/actual")]
        public async Task<ActionResult> GetToday()
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.NowColombia);
            var r = await _service.GetByDateAsync(today);
            return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error);
        }

        // ── GET /api/GastosExternos/rango?from=&to= ──────────────────────────
        [HttpGet("rango")]
        public async Task<ActionResult> GetByDateRange([FromQuery] string from, [FromQuery] string to)
        {
            if (!DateOnly.TryParse(from, out var fromDate) || !DateOnly.TryParse(to, out var toDate))
                return BadRequest("Formato de fechas inválido. Use yyyy-MM-dd.");

            var r = await _service.GetByDateRangeAsync(fromDate, toDate);
            return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error);
        }

        // ── PUT /api/GastosExternos/{id} ─────────────────────────────────────
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,admin,super_admin")]
        public async Task<ActionResult> Update(int id, [FromBody] GastoExternoInput input)
        {
            // Obtener usuarioId del token o buscar por email para compatibilidad
            if (input.UsuarioId <= 0)
            {
                var email = User.FindFirst("email")?.Value
                            ?? User.FindFirst(ClaimTypes.Email)?.Value;

                int usuarioId = 0;

                // Intentar con NameIdentifier primero
                var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(usuarioIdStr, out var parsedId) && parsedId > 0)
                {
                    usuarioId = parsedId;
                }
                // Si no, buscar en BD por email
                else if (!string.IsNullOrWhiteSpace(email))
                {
                    var usuario = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Correo == email);
                    if (usuario != null)
                    {
                        usuarioId = usuario.Id;
                    }
                }

                if (usuarioId <= 0)
                {
                    return BadRequest("No se pudo identificar el usuario autenticado. Por favor, cierra sesión y vuelve a iniciar sesión.");
                }

                input.UsuarioId = usuarioId;
            }

            var r = await _service.UpdateAsync(id, input);
            return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error);
        }

        // ── DELETE /api/GastosExternos/{id} (soft delete) ───────────────────
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,admin,super_admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var r = await _service.DeleteAsync(id);
            if (!r.Success) return StatusCode(r.StatusCode, r.Error);
            return r.Data ? NoContent() : NotFound();
        }
    }
}
