namespace BarberiaApi.Middlewares;

/// <summary>
/// Middleware global que captura excepciones no controladas.
/// En producción solo devuelve el traceId (no el mensaje interno).
/// En desarrollo incluye el detalle para facilitar el debugging.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;
            _logger.LogError(ex, "Error no controlado. TraceId={TraceId} Path={Path}", traceId, context.Request.Path);

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            // En desarrollo incluir el detalle; en producción solo el traceId
            object response = _env.IsDevelopment()
                ? new { error = "Error interno del servidor", traceId, detail = ex.Message }
                : new { error = "Ocurrió un error procesando la solicitud.", traceId };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
