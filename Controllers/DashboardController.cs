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
    }
}
