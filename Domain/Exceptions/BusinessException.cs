namespace BarberiaApi.Domain.Exceptions;

/// <summary>
/// Se lanza cuando una operación viola una regla de negocio.
/// Se puede mapear a un 400 Bad Request en el <see cref="ExceptionMiddleware"/>.
/// </summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}
