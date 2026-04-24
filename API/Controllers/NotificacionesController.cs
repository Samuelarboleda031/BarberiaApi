using BarberiaApi.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberiaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificacionesController : ControllerBase
{
    private readonly IEmailProxyService _emailProxyService;

    public NotificacionesController(IEmailProxyService emailProxyService)
    {
        _emailProxyService = emailProxyService;
    }

    [HttpPost("cancelacion-email")]
    public async Task<IActionResult> EnviarCancelacionEmail(
        [FromBody] CancelacionEmailProxyRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClienteEmail))
            return BadRequest(new { success = false, message = "clienteEmail es obligatorio." });

        if (string.IsNullOrWhiteSpace(request.ClienteNombre))
            request.ClienteNombre = "Cliente";

        if (string.IsNullOrWhiteSpace(request.BarberoNombre))
            request.BarberoNombre = "Tu barbero";

        if (string.IsNullOrWhiteSpace(request.Motivo))
            request.Motivo = "Cita cancelada por el administrador/barbero.";

        var resultado = await _emailProxyService.EnviarCancelacionAsync(request, cancellationToken);
        if (!resultado.Enviado)
        {
            return StatusCode(502, new
            {
                success = false,
                message = resultado.Mensaje,
                status = resultado.CodigoRespuesta
            });
        }

        return Ok(new
        {
            success = true,
            message = resultado.Mensaje,
            status = resultado.CodigoRespuesta
        });
    }
}
