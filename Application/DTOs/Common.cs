using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace BarberiaApi.Application.DTOs;

// DTOs Comunes
public class CambioEstadoInput
{
    public string estado { get; set; } = string.Empty;
}

public class CambioEstadoBooleanInput
{
    public bool estado { get; set; }
}

public class CambioEstadoResponse<T>
{
    public T entidad { get; set; } = default!;
    public string mensaje { get; set; } = string.Empty;
    public bool exitoso { get; set; } = false;
}

// ServiceResult para respuestas uniformes desde los servicios
public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public int StatusCode { get; set; } = 200;

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ServiceResult<T> Fail(string error, int statusCode = 400)
        => new() { Success = false, Error = error, StatusCode = statusCode };
    public static ServiceResult<T> NotFound(string error = "Recurso no encontrado")
        => new() { Success = false, Error = error, StatusCode = 404 };
}
