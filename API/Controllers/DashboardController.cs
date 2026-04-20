using BarberiaApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        public DashboardController(IDashboardService dashboardService) => _dashboardService = dashboardService;

        [HttpGet] [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> Get()
        { var r = await _dashboardService.GetDashboardAsync(); return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error); }

        [HttpGet("ganancias")]
        [OutputCache(PolicyName = "short")]
        public async Task<ActionResult> GetGanancias([FromQuery] string periodo = "hoy", [FromQuery] string barbero = "Todos")
        {
            var r = await _dashboardService.GetGananciasAsync(periodo, barbero);
            return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error);
        }
    }
}
