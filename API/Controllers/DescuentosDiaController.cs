using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DescuentosDiaController : ControllerBase
    {
        private readonly IDescuentoDiaService _service;
        public DescuentosDiaController(IDescuentoDiaService service) => _service = service;

        // GET disponible para todo usuario autenticado: front y móvil necesitan leer
        // los descuentos para mostrarlos y aplicarlos al completar citas.
        [HttpGet]
        public async Task<ActionResult> GetAll()
        { var r = await _service.GetAllAsync(); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpPut]
        [Authorize(Roles = "Admin,admin,super_admin")]
        public async Task<ActionResult> Upsert([FromBody] DescuentoDiaInput input)
        { var r = await _service.UpsertAsync(input); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpDelete("{fecha}")]
        [Authorize(Roles = "Admin,admin,super_admin")]
        public async Task<ActionResult> Delete(string fecha)
        { var r = await _service.DeleteAsync(fecha); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }
    }
}
