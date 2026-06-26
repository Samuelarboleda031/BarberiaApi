using BarberiaApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly IGastoExternoService _gastoExternoService;

        public DashboardController(IDashboardService dashboardService, IGastoExternoService gastoExternoService)
        {
            _dashboardService = dashboardService;
            _gastoExternoService = gastoExternoService;
        }

        [HttpGet] [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> Get()
        { var r = await _dashboardService.GetDashboardAsync(); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpGet("ganancias")]
        public async Task<ActionResult> GetGanancias([FromQuery] string periodo = "hoy", [FromQuery] string barbero = "Todos")
        {
            var r = await _dashboardService.GetGananciasAsync(periodo, barbero);
            return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error);
        }

        [HttpGet("resumen-dia")]
        public async Task<ActionResult> GetResumenDia([FromQuery] string? fecha = null)
        {
            DateOnly targetDate;
            if (fecha != null && DateOnly.TryParse(fecha, out var parsed))
            {
                targetDate = parsed;
            }
            else
            {
                targetDate = DateOnly.FromDateTime(DateTime.Today);
            }

            var r = await _gastoExternoService.GetResumenDiaAsync(targetDate);
            return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error);
        }
    }
}
