using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberiaApi.Controllers;

[ApiController]
[Route("api/credito-barbero")]
[Authorize]
public class CreditoBarberoController : ControllerBase
{
    private readonly ICreditoBarberoService _service;

    public CreditoBarberoController(ICreditoBarberoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? q = null, [FromQuery] string? estado = null)
    {
        var r = await _service.GetAllAsync(page, pageSize, q, estado);
        return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error);
    }

    [HttpGet("barbero/{barberoId}")]
    public async Task<ActionResult> GetByBarbero(int barberoId)
    {
        var r = await _service.GetByBarberoAsync(barberoId);
        return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error);
    }

    [HttpGet("barbero/{barberoId}/abonos")]
    public async Task<ActionResult> GetAbonos(int barberoId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var r = await _service.GetAbonosAsync(barberoId, page, pageSize);
        return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error);
    }

    [HttpGet("{cicloId}/abonos")]
    public async Task<ActionResult> GetAbonosByCiclo(int cicloId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var r = await _service.GetAbonosByCicloAsync(cicloId, page, pageSize);
        return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error);
    }

    [HttpGet("barbero/{barberoId}/abonos/todos")]
    public async Task<ActionResult> GetAllAbonosByBarbero(int barberoId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var r = await _service.GetAllAbonosByBarberoAsync(barberoId, page, pageSize);
        return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error);
    }

    [HttpPost("barbero/{barberoId}/abono")]
    public async Task<ActionResult> RegistrarAbono(int barberoId, [FromBody] AbonoInput input)
    {
        var r = await _service.RegistrarAbonoAsync(barberoId, input);
        return r.Success ? Ok(r.Data) : StatusCode(r.StatusCode, r.Error);
    }

    [HttpPost("barbero/{barberoId}/inicializar")]
    public async Task<ActionResult> Inicializar(int barberoId)
    {
        var r = await _service.GetOrCreateAsync(barberoId);
        return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound() : BadRequest(r.Error);
    }

[HttpPut("barbero/{barberoId}/extender-plazo")]
    public async Task<ActionResult> ExtenderPlazo(int barberoId, [FromBody] ExtenderPlazoInput input)
    {
        var r = await _service.ExtenderPlazoAsync(barberoId, input);
        return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound(r.Error) : BadRequest(r.Error);
    }

    [HttpPost("barbero/{barberoId}/nuevo-ciclo")]
    public async Task<ActionResult> NuevoCiclo(int barberoId, [FromBody] NuevoCicloInput input)
    {
        var r = await _service.NuevoCicloAsync(barberoId, input);
        return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound(r.Error) : BadRequest(r.Error);
    }

    [HttpPut("barbero/{barberoId}/subir-limite")]
    public async Task<ActionResult> SubirLimite(int barberoId, [FromBody] SubirLimiteInput input)
    {
        var r = await _service.SubirLimiteAsync(barberoId, input);
        return r.Success ? Ok(r.Data) : r.StatusCode == 404 ? NotFound(r.Error) : BadRequest(r.Error);
    }
}
