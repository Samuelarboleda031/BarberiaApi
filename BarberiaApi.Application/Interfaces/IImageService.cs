using BarberiaApi.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace BarberiaApi.Application.Interfaces;

public interface IImageService
{
    Task<ServiceResult<object>> SubirImagenAsync(IFormFile imagen, int? productoId, int? usuarioId);
    Task<ServiceResult<object>> EliminarImagenProductoAsync(int id, bool borrarCloud);
    Task<ServiceResult<object>> EliminarImagenUsuarioAsync(int id, bool borrarCloud);
    Task<ServiceResult<object>> SubirImagenProductoAsync(int id, IFormFile imagen);
    Task<ServiceResult<object>> EliminarImagenProductoDirectaAsync(int id, bool borrarCloud);
    Task<ServiceResult<object>> SubirImagenServicioAsync(int id, IFormFile imagen);
    Task<ServiceResult<object>> EliminarImagenServicioAsync(int id, bool borrarCloud);
    Task<ServiceResult<object>> SubirFotoUsuarioAsync(int id, IFormFile imagen);
    Task<ServiceResult<object>> EliminarFotoUsuarioAsync(int id, bool borrarCloud);
}
