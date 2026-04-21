namespace BarberiaApi.Middlewares;

/// <summary>
/// Middleware global que captura cualquier excepción no controlada y retorna
/// una respuesta JSON uniforme con código 500, evitando que el detalle del
/// error se exponga al cliente en producción.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Path}", context.Request.Path);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Error interno del servidor",
                detail = ex.Message
            });
        }
    }
}
