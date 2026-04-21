namespace BarberiaApi.Domain.Exceptions;

/// <summary>
/// Se lanza cuando un recurso solicitado no existe en la base de datos.
/// El <see cref="ExceptionMiddleware"/> lo puede capturar y retornar 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} con ID {key} no fue encontrado") { }
}
